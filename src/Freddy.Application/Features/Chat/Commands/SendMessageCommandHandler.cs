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
    IPackageRouter packageRouter,
    ILogger<SendMessageCommandHandler> logger) : IRequestHandler<SendMessageCommand, Result<MessageDto>>
{
    private const double HighConfidenceThreshold = 0.8;

    public async Task<Result<MessageDto>> Handle(
        SendMessageCommand request,
        CancellationToken cancellationToken)
    {
        Conversation? conversation = await conversationRepository.GetByIdAsync(request.ConversationId, cancellationToken).ConfigureAwait(false);
        if (conversation is null)
        {
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

        // 2. Get all active packages as routing candidates
        IReadOnlyList<Package> packages = await packageRepository.GetAllActiveAsync(cancellationToken).ConfigureAwait(false);
        PackageCandidate[] candidates = [.. packages
            .Select(p => new PackageCandidate(p.Id, p.Name, p.Description)),];

        // 3. Route via Ollama (JSON classifier)
        PackageRouterResult routerResult = await packageRouter.RouteAsync(
            request.Content, candidates, cancellationToken).ConfigureAwait(false);

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
            "Message processed for conversation {ConversationId} — confidence: {Confidence:F2}, packageId: {PackageId}",
            request.ConversationId, routerResult.Confidence, routerResult.ChosenPackageId);

        return Result<MessageDto>.Success(new MessageDto(
            assistantMessage.Id,
            MapRole(assistantMessage.Role),
            assistantMessage.Content,
            assistantMessage.CreatedAt));
    }

    private async Task<string> BuildResponseAsync(PackageRouterResult routerResult, CancellationToken cancellationToken)
    {
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
            logger.LogInformation("Package match needs confirmation: {PackageName} (confidence: {Confidence:F2})", package.Name, routerResult.Confidence);
            return $"Ik denk dat je vraag gaat over **{package.Name}**. Klopt dat?\n\n" +
                   $"_{package.Description}_";
        }

        // High confidence — return package content directly
        logger.LogInformation("High confidence match: {PackageName} (confidence: {Confidence:F2})", package.Name, routerResult.Confidence);
        return $"**{package.Name}**\n\n{package.Content}";
    }

    private static string MapRole(MessageRole role) => role switch
    {
        MessageRole.User => "user",
        MessageRole.Assistant => "assistant",
        _ => "unknown",
    };
}
