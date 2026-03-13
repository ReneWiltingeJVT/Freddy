using System.Text.Json;
using Freddy.Application.Common;
using Freddy.Application.Common.Interfaces;
using Freddy.Application.Entities;
using Freddy.Application.Features.Chat.DTOs;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Freddy.Application.Features.Chat.Commands;

public sealed class SendMessageCommandHandler(
    IConversationRepository conversationRepository,
    IPackageRepository packageRepository,
    IDocumentRepository documentRepository,
    IPackageRouter packageRouter,
    ISmallTalkDetector smallTalkDetector,
    IClientDetector clientDetector,
    IOverviewQueryDetector overviewQueryDetector,
    ILogger<SendMessageCommandHandler> logger) : IRequestHandler<SendMessageCommand, Result<MessageDto>>
{
    private const double HighConfidenceThreshold = 0.8;

    // Dutch/English words that count as "yes, that's correct"
    private static readonly HashSet<string> AffirmativeWords =
        ["ja", "klopt", "correct", "yes", "goed", "inderdaad", "juist", "precies", "yep", "jep", "yup"];

    // Dutch/English words that count as "no, that's wrong"
    private static readonly HashSet<string> NegativeWords =
        ["nee", "neen", "no", "niet", "verkeerd", "anders", "fout", "onjuist"];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>Bundles content text with optional structured document attachments.</summary>
    private sealed record AssistantResponse(
        string Content,
        IReadOnlyList<AttachmentDto>? Attachments = null);

    private static string? SerializeAttachments(IReadOnlyList<AttachmentDto>? attachments) =>
        attachments is { Count: > 0 }
            ? JsonSerializer.Serialize(attachments, JsonOptions)
            : null;

    public async Task<Result<MessageDto>> Handle(
        SendMessageCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "[DEBUG] SendMessageHandler — Start. ConversationId: {ConversationId}, Content: {Content}",
            request.ConversationId, request.Content);

        Conversation? conversation = await conversationRepository
            .GetByIdAsync(request.ConversationId, cancellationToken).ConfigureAwait(false);

        if (conversation is null)
        {
            logger.LogWarning(
                "[DEBUG] SendMessageHandler — Conversation {ConversationId} NOT FOUND",
                request.ConversationId);
            return Result<MessageDto>.NotFound($"Conversation {request.ConversationId} not found.");
        }

        // 1. Save user message
        string trimmedContent = request.Content.Trim();
        var userMessage = new Message
        {
            Id = Guid.CreateVersion7(),
            ConversationId = request.ConversationId,
            Role = MessageRole.User,
            Content = trimmedContent,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _ = await conversationRepository.AddMessageAsync(userMessage, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("[DEBUG] SendMessageHandler — User message saved: {MessageId}", userMessage.Id);

        // 2. Small talk fast-path: skip routing entirely
        SmallTalkResult smallTalkResult = smallTalkDetector.Detect(trimmedContent);
        if (smallTalkResult.IsSmallTalk)
        {
            logger.LogInformation(
                "[SmallTalk] Matched category {Category} for conversation {ConversationId}",
                smallTalkResult.Category,
                request.ConversationId);

            var smallTalkMessage = new Message
            {
                Id = Guid.CreateVersion7(),
                ConversationId = request.ConversationId,
                Role = MessageRole.Assistant,
                Content = smallTalkResult.TemplateResponse!,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            _ = await conversationRepository.AddMessageAsync(smallTalkMessage, cancellationToken).ConfigureAwait(false);

            return Result<MessageDto>.Success(new MessageDto(
                smallTalkMessage.Id,
                MapRole(smallTalkMessage.Role),
                smallTalkMessage.Content,
                smallTalkMessage.CreatedAt));
        }

        // 4. Overview query fast-path: answer count/list questions without full routing
        AssistantResponse? overviewResponse = await TryHandleOverviewQueryAsync(
            conversation, trimmedContent, cancellationToken).ConfigureAwait(false);

        if (overviewResponse is not null)
        {
            logger.LogInformation(
                "[OverviewQuery] Answered overview query for conversation {ConversationId}",
                request.ConversationId);

            var overviewMessage = new Message
            {
                Id = Guid.CreateVersion7(),
                ConversationId = request.ConversationId,
                Role = MessageRole.Assistant,
                Content = overviewResponse.Content,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            _ = await conversationRepository.AddMessageAsync(overviewMessage, cancellationToken).ConfigureAwait(false);

            return Result<MessageDto>.Success(new MessageDto(
                overviewMessage.Id,
                MapRole(overviewMessage.Role),
                overviewMessage.Content,
                overviewMessage.CreatedAt));
        }

        // 4. Dispatch based on pending state
        AssistantResponse response = conversation.PendingState switch
        {
            ConversationPendingState.AwaitingPackageConfirmation =>
                await HandlePackageConfirmationAsync(conversation, trimmedContent, cancellationToken)
                    .ConfigureAwait(false),

            ConversationPendingState.AwaitingDocumentDelivery =>
                await HandleDocumentConfirmationAsync(conversation, trimmedContent, cancellationToken)
                    .ConfigureAwait(false),
            ConversationPendingState.AwaitingClientConfirmation => throw new NotImplementedException(),
            _ => await RouteAndBuildResponseAsync(conversation, trimmedContent, cancellationToken)
                    .ConfigureAwait(false),
        };

        // 3. Save assistant message
        var assistantMessage = new Message
        {
            Id = Guid.CreateVersion7(),
            ConversationId = request.ConversationId,
            Role = MessageRole.Assistant,
            Content = response.Content,
            AttachmentsJson = SerializeAttachments(response.Attachments),
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _ = await conversationRepository.AddMessageAsync(assistantMessage, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "[DEBUG] SendMessageHandler — COMPLETE. ConversationId={ConversationId}, ResponseLength={ResponseLength}",
            request.ConversationId, response.Content.Length);

        return Result<MessageDto>.Success(new MessageDto(
            assistantMessage.Id,
            MapRole(assistantMessage.Role),
            assistantMessage.Content,
            assistantMessage.CreatedAt,
            response.Attachments));
    }

    // -----------------------------------------------------------------------
    // Pending state handlers
    // -----------------------------------------------------------------------

    /// <summary>
    /// User was asked to confirm a package match ("Ik denk dat je vraag gaat over X. Klopt dat?").
    /// </summary>
    private async Task<AssistantResponse> HandlePackageConfirmationAsync(
        Conversation conversation,
        string userInput,
        CancellationToken cancellationToken)
    {
        Guid pendingId = conversation.PendingPackageId!.Value;
        bool isYes = IsAffirmative(userInput);
        bool isNo = IsNegative(userInput);

        logger.LogInformation(
            "[Confirmation] AwaitingPackageConfirmation — PackageId={PackageId}, IsYes={IsYes}, IsNo={IsNo}",
            pendingId, isYes, isNo);

        if (isYes)
        {
            Package? package = await packageRepository
                .GetByIdAsync(pendingId, cancellationToken).ConfigureAwait(false);

            if (package is null)
            {
                await ClearPendingAsync(conversation.Id, cancellationToken).ConfigureAwait(false);
                return new AssistantResponse("Sorry, er is een fout opgetreden bij het ophalen van de informatie. Probeer het opnieuw.");
            }

            return await DeliverPackageAndMaybeOfferDocumentsAsync(
                conversation, package, cancellationToken).ConfigureAwait(false);
        }

        if (isNo)
        {
            await ClearPendingAsync(conversation.Id, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("[Confirmation] User denied package match ÔåÆ asking to rephrase");
            return new AssistantResponse("Geen probleem! Kun je je vraag iets anders omschrijven, dan help ik je verder.");
        }

        // Ambiguous — treat as a new question
        logger.LogInformation("[Confirmation] Ambiguous reply — routing as new question");
        await ClearPendingAsync(conversation.Id, cancellationToken).ConfigureAwait(false);
        return await RouteAndBuildResponseAsync(conversation, userInput, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// User was asked whether they want the documents for the previously delivered package.
    /// </summary>
    private async Task<AssistantResponse> HandleDocumentConfirmationAsync(
        Conversation conversation,
        string userInput,
        CancellationToken cancellationToken)
    {
        Guid pendingId = conversation.PendingPackageId!.Value;
        bool isYes = IsAffirmative(userInput);
        bool isNo = IsNegative(userInput);

        logger.LogInformation(
            "[DocumentOffer] AwaitingDocumentDelivery — PackageId={PackageId}, IsYes={IsYes}, IsNo={IsNo}",
            pendingId, isYes, isNo);

        await ClearPendingAsync(conversation.Id, cancellationToken).ConfigureAwait(false);

        if (isYes)
        {
            IReadOnlyList<Document> documents = await documentRepository
                .GetByPackageIdAsync(pendingId, cancellationToken).ConfigureAwait(false);

            return documents.Count == 0
                ? new AssistantResponse("Er zijn momenteel geen documenten beschikbaar voor dit protocol.")
                : BuildDocumentResponse(documents);
        }

        if (isNo)
        {
            return new AssistantResponse("Geen probleem! Roep me gerust als je nog vragen hebt.");
        }

        // Ambiguous — treat as a new question
        logger.LogInformation("[DocumentOffer] Ambiguous reply — routing as new question");
        return await RouteAndBuildResponseAsync(conversation, userInput, cancellationToken).ConfigureAwait(false);
    }

    // -----------------------------------------------------------------------
    // Routing
    // -----------------------------------------------------------------------

    private async Task<AssistantResponse> RouteAndBuildResponseAsync(
        Conversation conversation,
        string userInput,
        CancellationToken cancellationToken)
    {
        // Detect client from current message
        ClientDetectionResult clientResult = await clientDetector
            .DetectAsync(userInput, cancellationToken).ConfigureAwait(false);

        // If no client detected in this message, check if a client was already identified this conversation
        Guid? effectiveClientId = clientResult.IsDetected ? clientResult.ClientId : conversation.PendingClientId;
        string? effectiveClientName = clientResult.IsDetected ? clientResult.MatchedName : null;

        // Persist newly detected client for multi-turn context
        if (clientResult.IsDetected && conversation.PendingClientId != clientResult.ClientId)
        {
            await conversationRepository
                .SetPendingClientIdAsync(conversation.Id, clientResult.ClientId, cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "[Routing] Persisting PendingClientId={ClientId} for conversation {ConversationId}",
                clientResult.ClientId, conversation.Id);
        }

        IReadOnlyList<Package> packages;
        if (effectiveClientId.HasValue)
        {
            // Client context: include personal plan packages scoped to this client
            // alongside all general published packages (Protocol + WorkInstruction)
            IReadOnlyList<Package> generalPackages = await packageRepository
                .GetAllPublishedAsync(cancellationToken).ConfigureAwait(false);
            IReadOnlyList<Package> clientPackages = await packageRepository
                .GetPublishedByClientIdAsync(effectiveClientId.Value, cancellationToken).ConfigureAwait(false);

            var merged = new List<Package>(generalPackages.Where(p => p.Category != PackageCategory.PersonalPlan));
            merged.AddRange(clientPackages);
            packages = merged;

            logger.LogInformation(
                "[Routing] Client context: {ClientName} ({ClientId}) — {General} general + {Personal} personal plan packages",
                effectiveClientName ?? "(from conversation)", effectiveClientId,
                merged.Count - clientPackages.Count, clientPackages.Count);
        }
        else
        {
            IReadOnlyList<Package> allPublished = await packageRepository
                .GetAllPublishedAsync(cancellationToken).ConfigureAwait(false);
            packages = [.. allPublished.Where(p => p.Category != PackageCategory.PersonalPlan)];
        }

        logger.LogInformation("[DEBUG] SendMessageHandler — Found {PackageCount} candidate packages", packages.Count);

        // Batch-load document names for all candidate packages in a single query (avoids N+1)
        Dictionary<Guid, List<string>> documentNamesByPackage = await documentRepository
            .GetNamesByPackageIdsAsync(packages.Select(p => p.Id), cancellationToken)
            .ConfigureAwait(false);

        PackageCandidate[] candidates = [.. packages
            .Select(p => new PackageCandidate(
                p.Id, p.Title, p.Description,
                [.. p.Tags], [.. p.Synonyms],
                p.Content,
                documentNamesByPackage.TryGetValue(p.Id, out List<string>? names) ? names : [],
                p.Category)),];

        logger.LogInformation("[DEBUG] SendMessageHandler — Calling PackageRouter...");
        PackageRouterResult routerResult = await packageRouter
            .RouteAsync(userInput, candidates, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "[DEBUG] Router — PackageId={PackageId}, Confidence={Confidence:F2}, NeedsConfirmation={NeedsConfirmation}, ServiceUnavailable={ServiceUnavailable}",
            routerResult.ChosenPackageId, routerResult.Confidence,
            routerResult.NeedsConfirmation, routerResult.IsServiceUnavailable);

        // IsServiceUnavailable should no longer propagate (CompositePackageRouter handles it)
        // but guard defensively in case of future routing implementations
        if (routerResult.IsServiceUnavailable)
        {
            logger.LogWarning("[Routing] Received IsServiceUnavailable at handler level — returning suggestion fallback");
            return routerResult.SuggestedPackages is { Count: > 0 }
                ? BuildSuggestionResponse(routerResult.SuggestedPackages)
                : new AssistantResponse(
                "Ik kon geen passend antwoord vinden. Kun je je vraag anders formuleren of neem contact op met je leidinggevende.");
        }

        if (!routerResult.IsSuccessful || routerResult.ChosenPackageId is null)
        {
            logger.LogInformation("No match (confidence: {Confidence:F2})", routerResult.Confidence);

            // Build suggestion response if router provided suggestions
            return routerResult.SuggestedPackages is { Count: > 0 }
                ? BuildSuggestionResponse(routerResult.SuggestedPackages)
                : new AssistantResponse(
                "Sorry, ik kon geen antwoord vinden op je vraag. " +
                "Kun je je vraag anders formuleren of neem contact op met je leidinggevende.");
        }

        Package? package = await packageRepository
            .GetByIdAsync(routerResult.ChosenPackageId.Value, cancellationToken).ConfigureAwait(false);

        if (package is null)
        {
            logger.LogWarning("Router returned packageId {PackageId} not found in database", routerResult.ChosenPackageId.Value);
            return new AssistantResponse("Sorry, er is een fout opgetreden bij het ophalen van de informatie. Probeer het opnieuw.");
        }

        // Check if package requires confirmation AND router suggests it
        bool shouldAskConfirmation = package.RequiresConfirmation &&
                                     (routerResult.NeedsConfirmation || routerResult.Confidence < HighConfidenceThreshold);

        if (shouldAskConfirmation)
        {
            logger.LogInformation(
                "Match needs confirmation: {PackageName} (confidence: {Confidence:F2}, RequiresConfirmation: {RequiresConfirmation})",
                package.Title, routerResult.Confidence, package.RequiresConfirmation);

            await conversationRepository.SetPendingStateAsync(
                conversation.Id, package.Id,
                ConversationPendingState.AwaitingPackageConfirmation,
                cancellationToken).ConfigureAwait(false);

            return new AssistantResponse($"Ik denk dat je vraag gaat over **{package.Title}**. Klopt dat?\n\n_{package.Description}_");
        }

        // High confidence or confirmation not required — deliver directly
        logger.LogInformation(
            "Delivering package directly: {PackageName} (confidence: {Confidence:F2}, RequiresConfirmation: {RequiresConfirmation})",
            package.Title, routerResult.Confidence, package.RequiresConfirmation);
        return await DeliverPackageAndMaybeOfferDocumentsAsync(conversation, package, cancellationToken).ConfigureAwait(false);
    }

    // -----------------------------------------------------------------------
    // Overview query handler
    // -----------------------------------------------------------------------

    /// <summary>
    /// Handles count/list/personal-plan overview queries without going through full routing.
    /// Returns null if the message is not an overview query (caller should fall through to routing).
    /// </summary>
    private async Task<AssistantResponse?> TryHandleOverviewQueryAsync(
        Conversation conversation,
        string userInput,
        CancellationToken cancellationToken)
    {
        OverviewQueryIntent intent = overviewQueryDetector.Detect(userInput);
        if (!intent.IsOverview)
        {
            return null;
        }

        logger.LogInformation(
            "[OverviewQuery] Detected {QueryType} (category={Category}, clientHint='{Hint}')",
            intent.QueryType, intent.Category, intent.ClientNameHint);

        switch (intent.QueryType)
        {
            case OverviewQueryType.CountByCategory:
                {
                    if (intent.Category is null)
                    {
                        break;
                    }

                    IReadOnlyList<Package> packages = await packageRepository
                        .GetAllPublishedByCategoryAsync(intent.Category.Value, cancellationToken)
                        .ConfigureAwait(false);

                    string categoryName = CategoryDisplayName(intent.Category.Value);
                    if (packages.Count == 0)
                    {
                        return new AssistantResponse($"Er zijn momenteel geen gepubliceerde {categoryName.ToLower()}s.");
                    }

                    string titles = string.Join(", ", packages.Select(p => $"**{p.Title}**"));
                    string noun = packages.Count == 1 ? categoryName.ToLower() : $"{categoryName.ToLower()}s";
                    return new AssistantResponse(
                        $"Er {(packages.Count == 1 ? "is" : "zijn")} momenteel **{packages.Count}** {noun}: {titles}.");
                }

            case OverviewQueryType.ListByCategory:
                {
                    if (intent.Category is null)
                    {
                        break;
                    }

                    IReadOnlyList<Package> packages = await packageRepository
                        .GetAllPublishedByCategoryAsync(intent.Category.Value, cancellationToken)
                        .ConfigureAwait(false);

                    string categoryName = CategoryDisplayName(intent.Category.Value);
                    if (packages.Count == 0)
                    {
                        return new AssistantResponse($"Er zijn momenteel geen gepubliceerde {categoryName.ToLower()}s.");
                    }

                    string list = string.Join("\n", packages.Select(p => $"- **{p.Title}** \u2014 {p.Description}"));
                    return new AssistantResponse($"Dit zijn de beschikbare {categoryName.ToLower()}s:\n\n{list}");
                }

            case OverviewQueryType.ListAll:
                {
                    IReadOnlyList<Package> allPublished = await packageRepository
                        .GetAllPublishedAsync(cancellationToken)
                        .ConfigureAwait(false);

                    List<Package> generalPackages = [.. allPublished.Where(p => p.Category != PackageCategory.PersonalPlan)];

                    if (generalPackages.Count == 0)
                    {
                        return new AssistantResponse("Er zijn momenteel geen gepubliceerde pakketten.");
                    }

                    IOrderedEnumerable<IGrouping<PackageCategory, Package>> grouped = generalPackages
                        .GroupBy(p => p.Category)
                        .OrderBy(g => (int)g.Key);

                    var sb = new System.Text.StringBuilder("Dit zijn alle beschikbare pakketten:\n");
                    foreach (IGrouping<PackageCategory, Package>? group in grouped)
                    {
                        _ = sb.Append($"\n**{CategoryDisplayName(group.Key)}s:**\n");
                        foreach (Package p in group)
                        {
                            _ = sb.Append($"- {p.Title}\n");
                        }
                    }

                    return new AssistantResponse(sb.ToString().TrimEnd());
                }

            case OverviewQueryType.PersonalPlansForClient:
                {
                    Guid? clientId = null;
                    string? clientName = null;

                    // Try to find the client from the extracted name hint
                    if (intent.ClientNameHint is not null)
                    {
                        ClientDetectionResult hintResult = await clientDetector
                            .DetectAsync(intent.ClientNameHint, cancellationToken).ConfigureAwait(false);

                        if (hintResult.IsDetected)
                        {
                            clientId = hintResult.ClientId;
                            clientName = hintResult.MatchedName;

                            // Persist for future turns
                            if (conversation.PendingClientId != clientId)
                            {
                                await conversationRepository
                                    .SetPendingClientIdAsync(conversation.Id, clientId, cancellationToken)
                                    .ConfigureAwait(false);
                            }
                        }
                    }

                    // Fall back to conversation's persisted client
                    if (clientId is null && conversation.PendingClientId.HasValue)
                    {
                        clientId = conversation.PendingClientId;
                    }

                    if (clientId is null)
                    {
                        return new AssistantResponse(
                            "Ik herkende geen bekende cliënt in je vraag. Kun je de naam van de cliënt vermelden?");
                    }

                    IReadOnlyList<Package> plans = await packageRepository
                        .GetPublishedByClientIdAsync(clientId.Value, cancellationToken)
                        .ConfigureAwait(false);

                    if (plans.Count == 0)
                    {
                        return new AssistantResponse(
                            $"Er zijn geen persoonlijke plannen beschikbaar voor {clientName ?? "deze cliënt"}.");
                    }

                    string planList = string.Join("\n", plans.Select(p => $"- **{p.Title}** \u2014 {p.Description}"));
                    return new AssistantResponse(
                        $"Dit zijn de persoonlijke plannen voor **{clientName ?? "de cliënt"}**:\n\n{planList}");
                }

            case OverviewQueryType.None:
                break;
            default:
                break;
        }

        return null;
    }

    private static string CategoryDisplayName(PackageCategory category) => category switch
    {
        PackageCategory.Protocol => "Protocol",
        PackageCategory.WorkInstruction => "Werkinstructie",
        PackageCategory.PersonalPlan => "Persoonlijk plan",
        _ => "Pakket",
    };


    // -----------------------------------------------------------------------
    // Content builders
    // -----------------------------------------------------------------------

    /// <summary>
    /// Delivers package content. If documents exist, appends an offer to receive them
    /// and sets the conversation to AwaitingDocumentDelivery.
    /// </summary>
    private async Task<AssistantResponse> DeliverPackageAndMaybeOfferDocumentsAsync(
        Conversation conversation,
        Package package,
        CancellationToken cancellationToken)
    {
        string content = $"**{package.Title}**\n\n{package.Content}";

        IReadOnlyList<Document> documents = await documentRepository
            .GetByPackageIdAsync(package.Id, cancellationToken).ConfigureAwait(false);

        if (documents.Count > 0)
        {
            await conversationRepository.SetPendingStateAsync(
                conversation.Id, package.Id,
                ConversationPendingState.AwaitingDocumentDelivery,
                cancellationToken).ConfigureAwait(false);

            string docNames = documents.Count == 1
                ? $"**{documents[0].Name}**"
                : string.Join(", ", documents.Select(d => $"**{d.Name}**"));

            content += $"\n\n---\n\nEr {(documents.Count == 1 ? "is" : "zijn")} ook {(documents.Count == 1 ? "een document" : $"{documents.Count} documenten")} beschikbaar ({docNames}). Wil je {(documents.Count == 1 ? "dit" : "deze")} ontvangen?";
        }
        else
        {
            await ClearPendingAsync(conversation.Id, cancellationToken).ConfigureAwait(false);
        }

        return new AssistantResponse(content);
    }

    /// <summary>Builds a structured document response with download attachments.</summary>
    private static AssistantResponse BuildDocumentResponse(IReadOnlyList<Document> documents)
    {
        string content = documents.Count == 1
            ? "Hier is het gevraagde document:"
            : $"Hier zijn de {documents.Count} gevraagde documenten:";

        var attachments = documents
            .Where(d => d.FileUrl is not null)
            .Select(d => new AttachmentDto(d.Name, d.FileUrl!, d.Description))
            .ToList();

        return new AssistantResponse(content, attachments.Count > 0 ? attachments : null);
    }

    /// <summary>
    /// Builds a suggestion response listing the top-3 packages when no exact match was found.
    /// </summary>
    private static AssistantResponse BuildSuggestionResponse(IReadOnlyList<SuggestedPackage> suggestions)
    {
        string content = "Ik kon geen exact antwoord vinden op je vraag. Misschien helpt een van deze onderwerpen:\n";

        foreach (SuggestedPackage suggestion in suggestions)
        {
            content += $"\n- 📦 **{suggestion.Title}** — {suggestion.Description}";
        }

        content += "\n\nOf stel je vraag anders, bijvoorbeeld met andere zoekwoorden.";

        return new AssistantResponse(content);
    }

    private Task ClearPendingAsync(Guid conversationId, CancellationToken cancellationToken) =>
        conversationRepository.SetPendingStateAsync(
            conversationId, packageId: null, ConversationPendingState.None, cancellationToken);

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static bool IsAffirmative(string input)
    {
        string[] words = input.ToLowerInvariant()
            .Split([' ', '.', '!', ','], StringSplitOptions.RemoveEmptyEntries);
        return words.Any(AffirmativeWords.Contains);
    }

    private static bool IsNegative(string input)
    {
        string[] words = input.ToLowerInvariant()
            .Split([' ', '.', '!', ','], StringSplitOptions.RemoveEmptyEntries);
        return words.Any(NegativeWords.Contains);
    }

    private static string MapRole(MessageRole role) => role switch
    {
        MessageRole.User => "user",
        MessageRole.Assistant => "assistant",
        _ => "unknown",
    };
}
