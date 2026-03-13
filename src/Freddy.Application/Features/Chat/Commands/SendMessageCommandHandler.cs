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
    IKnowledgeContextBuilder knowledgeContextBuilder,
    IChatResponseGenerator chatResponseGenerator,
    ILogger<SendMessageCommandHandler> logger) : IRequestHandler<SendMessageCommand, Result<MessageDto>>
{
    private const double HighConfidenceThreshold = 0.8;
    private const int MaxHistoryMessages = 10;

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
            "[Chat] Start. ConversationId: {ConversationId}, Content: {Content}",
            request.ConversationId, request.Content);

        Conversation? conversation = await conversationRepository
            .GetByIdAsync(request.ConversationId, cancellationToken).ConfigureAwait(false);

        if (conversation is null)
        {
            logger.LogWarning(
                "[Chat] Conversation {ConversationId} NOT FOUND",
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
        logger.LogInformation("[Chat] User message saved: {MessageId}", userMessage.Id);

        // 2. Small talk fast-path: skip routing entirely (<1ms)
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

        // 3. Dispatch based on pending state or route + generate conversational response
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

        // 4. Save assistant message
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
            "[Chat] COMPLETE. ConversationId={ConversationId}, ResponseLength={ResponseLength}",
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

            return await DeliverPackageWithLlmAsync(conversation, package, userInput, cancellationToken)
                .ConfigureAwait(false);
        }

        if (isNo)
        {
            await ClearPendingAsync(conversation.Id, cancellationToken).ConfigureAwait(false);
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
    // Routing + Conversational Response
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

        logger.LogInformation("[Routing] Found {PackageCount} candidate packages", packages.Count);

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

        logger.LogInformation("[Routing] Calling PackageRouter...");
        PackageRouterResult routerResult = await packageRouter
            .RouteAsync(userInput, candidates, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "[Routing] Result — PackageId={PackageId}, Confidence={Confidence:F2}, NeedsConfirmation={NeedsConfirmation}",
            routerResult.ChosenPackageId, routerResult.Confidence, routerResult.NeedsConfirmation);

        // Resolve matched package (if any)
        Package? matchedPackage = null;
        if (routerResult.IsSuccessful && routerResult.ChosenPackageId is not null)
        {
            matchedPackage = await packageRepository
                .GetByIdAsync(routerResult.ChosenPackageId.Value, cancellationToken).ConfigureAwait(false);
        }

        // Check if matched package requires confirmation
        if (matchedPackage is not null)
        {
            bool shouldAskConfirmation = matchedPackage.RequiresConfirmation &&
                                         (routerResult.NeedsConfirmation || routerResult.Confidence < HighConfidenceThreshold);

            if (shouldAskConfirmation)
            {
                logger.LogInformation(
                    "[Routing] Match needs confirmation: {PackageName} (confidence: {Confidence:F2})",
                    matchedPackage.Title, routerResult.Confidence);

                await conversationRepository.SetPendingStateAsync(
                    conversation.Id, matchedPackage.Id,
                    ConversationPendingState.AwaitingPackageConfirmation,
                    cancellationToken).ConfigureAwait(false);

                return new AssistantResponse(
                    $"Ik denk dat je vraag gaat over **{matchedPackage.Title}**. Klopt dat?\n\n_{matchedPackage.Description}_");
            }
        }

        // Build knowledge context and generate conversational response via LLM
        KnowledgeContext knowledgeContext = await knowledgeContextBuilder
            .BuildAsync(effectiveClientId, cancellationToken).ConfigureAwait(false);

        IReadOnlyList<Message> history = await conversationRepository
            .GetRecentMessagesAsync(conversation.Id, MaxHistoryMessages, cancellationToken)
            .ConfigureAwait(false);

        var chatRequest = new ChatResponseRequest(
            userInput,
            knowledgeContext,
            matchedPackage?.Title,
            matchedPackage?.Content,
            history);

        ChatResponseResult chatResult = await chatResponseGenerator
            .GenerateAsync(chatRequest, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "[Chat] LLM response generated. Grounded={IsGrounded}, Source={Source}, Length={Length}",
            chatResult.IsGrounded, chatResult.SourcePackageTitle, chatResult.Content.Length);

        // If we matched a package, offer documents after the conversational answer
        if (matchedPackage is not null)
        {
            return await AppendDocumentOfferAsync(conversation, matchedPackage, chatResult.Content, cancellationToken)
                .ConfigureAwait(false);
        }

        return new AssistantResponse(chatResult.Content);
    }

    // -----------------------------------------------------------------------
    // Package delivery with LLM
    // -----------------------------------------------------------------------

    /// <summary>
    /// Delivers package content through the LLM for a conversational answer,
    /// then optionally offers documents.
    /// Used when user confirms a pending package.
    /// </summary>
    private async Task<AssistantResponse> DeliverPackageWithLlmAsync(
        Conversation conversation,
        Package package,
        string userInput,
        CancellationToken cancellationToken)
    {
        await ClearPendingAsync(conversation.Id, cancellationToken).ConfigureAwait(false);

        Guid? effectiveClientId = conversation.PendingClientId;
        KnowledgeContext knowledgeContext = await knowledgeContextBuilder
            .BuildAsync(effectiveClientId, cancellationToken).ConfigureAwait(false);

        IReadOnlyList<Message> history = await conversationRepository
            .GetRecentMessagesAsync(conversation.Id, MaxHistoryMessages, cancellationToken)
            .ConfigureAwait(false);

        var chatRequest = new ChatResponseRequest(
            userInput,
            knowledgeContext,
            package.Title,
            package.Content,
            history);

        ChatResponseResult chatResult = await chatResponseGenerator
            .GenerateAsync(chatRequest, cancellationToken).ConfigureAwait(false);

        return await AppendDocumentOfferAsync(conversation, package, chatResult.Content, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Appends a document offer to the response if the package has documents.
    /// </summary>
    private async Task<AssistantResponse> AppendDocumentOfferAsync(
        Conversation conversation,
        Package package,
        string responseContent,
        CancellationToken cancellationToken)
    {
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

            responseContent += $"\n\n---\n\nEr {(documents.Count == 1 ? "is" : "zijn")} ook {(documents.Count == 1 ? "een document" : $"{documents.Count} documenten")} beschikbaar ({docNames}). Wil je {(documents.Count == 1 ? "dit" : "deze")} ontvangen?";
        }
        else
        {
            await ClearPendingAsync(conversation.Id, cancellationToken).ConfigureAwait(false);
        }

        return new AssistantResponse(responseContent);
    }

    // -----------------------------------------------------------------------
    // Content builders
    // -----------------------------------------------------------------------

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
