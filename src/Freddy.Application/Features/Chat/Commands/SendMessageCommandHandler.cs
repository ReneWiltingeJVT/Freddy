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
    ILogger<SendMessageCommandHandler> logger) : IRequestHandler<SendMessageCommand, Result<MessageDto>>
{
    private const double HighConfidenceThreshold = 0.8;

    public async Task<Result<MessageDto>> Handle(
        SendMessageCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("[DEBUG] SendMessageHandler — Start. ConversationId: {ConversationId}, Content: {Content}",
            request.ConversationId, request.Content);

        Conversation? conversation = await conversationRepository.GetByIdAsync(request.ConversationId, cancellationToken).ConfigureAwait(false);
        if (conversation is null)
        {
            logger.LogWarning("[DEBUG] SendMessageHandler — Conversation {ConversationId} NOT FOUND", request.ConversationId);
            return Result<MessageDto>.NotFound($"Conversation {request.ConversationId} not found.");
        }

        // 1. Save user message
        var userMessage = new Message
        {
            Id = Guid.CreateVersion7(),
            ConversationId = request.ConversationId,
            Role = MessageRole.User,
            Content = request.Content.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _ = await conversationRepository.AddMessageAsync(userMessage, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("[DEBUG] SendMessageHandler — User message saved: {MessageId}", userMessage.Id);

        // 2. Get all published packages as routing candidates
        IReadOnlyList<Package> packages = await packageRepository.GetAllPublishedAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("[DEBUG] SendMessageHandler — Found {PackageCount} published packages", packages.Count);
        PackageCandidate[] candidates = [.. packages
            .Select(p => new PackageCandidate(p.Id, p.Title, p.Description)),];

        // 3. Route via Ollama (JSON classifier)
        logger.LogInformation("[DEBUG] SendMessageHandler — Calling PackageRouter...");
        PackageRouterResult routerResult = await packageRouter.RouteAsync(
            request.Content, candidates, cancellationToken).ConfigureAwait(false);
        logger.LogInformation(
            "[DEBUG] SendMessageHandler — Router returned: PackageId={PackageId}, Confidence={Confidence:F2}, NeedsConfirmation={NeedsConfirmation}, ServiceUnavailable={ServiceUnavailable}, Reason={Reason}",
            routerResult.ChosenPackageId, routerResult.Confidence, routerResult.NeedsConfirmation,
            routerResult.IsServiceUnavailable, routerResult.Reason);

        // 4. Build response based on routing result
        string assistantContent = await BuildResponseAsync(routerResult, cancellationToken).ConfigureAwait(false);

        // 5. Save assistant message
        var assistantMessage = new Message
        {
            Id = Guid.CreateVersion7(),
            ConversationId = request.ConversationId,
            Role = MessageRole.Assistant,
            Content = assistantContent,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _ = await conversationRepository.AddMessageAsync(assistantMessage, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "[DEBUG] SendMessageHandler — COMPLETE. ConversationId={ConversationId}, Confidence={Confidence:F2}, PackageId={PackageId}, ResponseLength={ResponseLength}",
            request.ConversationId, routerResult.Confidence, routerResult.ChosenPackageId, assistantContent.Length);

        return Result<MessageDto>.Success(new MessageDto(
            assistantMessage.Id,
            MapRole(assistantMessage.Role),
            assistantMessage.Content,
            assistantMessage.CreatedAt));
    }

    private async Task<string> BuildResponseAsync(PackageRouterResult routerResult, CancellationToken cancellationToken)
    {
        // AI service is down — tell the user explicitly
        if (routerResult.IsServiceUnavailable)
        {
            logger.LogWarning("AI service unavailable — returning service error to user");
            return "Sorry, de AI-service is momenteel niet beschikbaar. Probeer het over enkele minuten opnieuw.";
        }

        // No match — confidence too low
        if (!routerResult.IsSuccessful || routerResult.ChosenPackageId is null)
        {
            logger.LogInformation("No package match (confidence: {Confidence:F2})", routerResult.Confidence);
            return "Sorry, ik kon geen passend pakket vinden voor je vraag. " +
                   "Kun je je vraag anders formuleren of neem contact op met je leidinggevende.";
        }

        // Validate: package must exist in our database
        Package? package = await packageRepository.GetByIdAsync(routerResult.ChosenPackageId.Value, cancellationToken).ConfigureAwait(false);
        if (package is null)
        {
            logger.LogWarning("Router returned packageId {PackageId} but it does not exist in database", routerResult.ChosenPackageId.Value);
            return "Sorry, er is een fout opgetreden bij het ophalen van de informatie. Probeer het opnieuw.";
        }

        // Needs confirmation — medium confidence
        if (routerResult.NeedsConfirmation || routerResult.Confidence < HighConfidenceThreshold)
        {
            logger.LogInformation("Package match needs confirmation: {PackageName} (confidence: {Confidence:F2})", package.Title, routerResult.Confidence);
            return $"Ik denk dat je vraag gaat over **{package.Title}**. Klopt dat?\n\n" +
                   $"_{package.Description}_";
        }

        // High confidence — return package content + documents
        logger.LogInformation("High confidence match: {PackageName} (confidence: {Confidence:F2})", package.Title, routerResult.Confidence);

        string response = $"**{package.Title}**\n\n{package.Content}";

        // Append documents if available
        IReadOnlyList<Document> documents = await documentRepository.GetByPackageIdAsync(
            package.Id, cancellationToken).ConfigureAwait(false);

        if (documents.Count > 0)
        {
            response += "\n\n📎 **Documenten:**";
            foreach (Document doc in documents)
            {
                string docLine = doc.FileUrl is not null
                    ? $"\n- [{doc.Name}]({doc.FileUrl})"
                    : $"\n- {doc.Name}";

                if (doc.Description is not null)
                {
                    docLine += $" — _{doc.Description}_";
                }

                response += docLine;
            }
        }

        return response;
    }

    private static string MapRole(MessageRole role) => role switch
    {
        MessageRole.User => "user",
        MessageRole.Assistant => "assistant",
        _ => "unknown",
    };
}
