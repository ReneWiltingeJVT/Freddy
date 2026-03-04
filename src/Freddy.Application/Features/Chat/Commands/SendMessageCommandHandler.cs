using Freddy.Application.Common;
using Freddy.Application.Common.Interfaces;
using Freddy.Application.Entities;
using Freddy.Application.Features.Chat.DTOs;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Freddy.Application.Features.Chat.Commands;

public sealed class SendMessageCommandHandler(
    IConversationRepository repository,
    IChatService chatService,
    ISmallTalkDetector smallTalkDetector,
    ILogger<SendMessageCommandHandler> logger) : IRequestHandler<SendMessageCommand, Result<MessageDto>>
{
    public async Task<Result<MessageDto>> Handle(
        SendMessageCommand request,
        CancellationToken cancellationToken)
    {
        Conversation? conversation = await repository.GetByIdAsync(request.ConversationId, cancellationToken).ConfigureAwait(false);
        if (conversation is null)
        {
            return Result<MessageDto>.NotFound($"Conversation {request.ConversationId} not found.");
        }

        string trimmedContent = request.Content.Trim();

        var userMessage = new Message
        {
            Id = Guid.CreateVersion7(),
            ConversationId = request.ConversationId,
            Role = MessageRole.User,
            Content = trimmedContent,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _ = await repository.AddMessageAsync(userMessage, cancellationToken).ConfigureAwait(false);

        SmallTalkResult smallTalkResult = smallTalkDetector.Detect(trimmedContent);

        string assistantContent = smallTalkResult.IsSmallTalk
            ? HandleSmallTalk(smallTalkResult, request.ConversationId)
            : await HandleLlmResponseAsync(trimmedContent, request.ConversationId, cancellationToken).ConfigureAwait(false);

        var assistantMessage = new Message
        {
            Id = Guid.CreateVersion7(),
            ConversationId = request.ConversationId,
            Role = MessageRole.Assistant,
            Content = assistantContent,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _ = await repository.AddMessageAsync(assistantMessage, cancellationToken).ConfigureAwait(false);

        string routingLane = smallTalkResult.IsSmallTalk ? "small-talk" : "llm";
        logger.LogInformation(
            "Message processed for conversation {ConversationId} via {RoutingLane}",
            request.ConversationId,
            routingLane);

        return Result<MessageDto>.Success(new MessageDto(
            assistantMessage.Id,
            MapRole(assistantMessage.Role),
            assistantMessage.Content,
            assistantMessage.CreatedAt));
    }

    private string HandleSmallTalk(SmallTalkResult result, Guid conversationId)
    {
        logger.LogInformation(
            "[SmallTalk] Matched category {Category} for conversation {ConversationId}",
            result.Category,
            conversationId);

        return result.TemplateResponse!;
    }

    private async Task<string> HandleLlmResponseAsync(
        string content,
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        Result<string> aiResult = await chatService.GetResponseAsync(content, cancellationToken).ConfigureAwait(false);

        if (aiResult.IsFailure)
        {
            logger.LogWarning("AI service failed for conversation {ConversationId}: {Error}", conversationId, aiResult.Error);
        }

        return aiResult.IsSuccess
            ? aiResult.Value!
            : "Sorry, ik kan je vraag momenteel niet verwerken. Probeer het later opnieuw.";
    }

    private static string MapRole(MessageRole role) => role switch
    {
        MessageRole.User => "user",
        MessageRole.Assistant => "assistant",
        _ => "unknown",
    };
}
