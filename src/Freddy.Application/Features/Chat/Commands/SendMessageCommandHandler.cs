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

        var userMessage = new Message
        {
            Id = Guid.CreateVersion7(),
            ConversationId = request.ConversationId,
            Role = MessageRole.User,
            Content = request.Content.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _ = await repository.AddMessageAsync(userMessage, cancellationToken).ConfigureAwait(false);

        Result<string> aiResult = await chatService.GetResponseAsync(request.Content, cancellationToken).ConfigureAwait(false);

        string assistantContent = aiResult.IsSuccess
            ? aiResult.Value!
            : "Sorry, ik kan je vraag momenteel niet verwerken. Probeer het later opnieuw.";

        if (aiResult.IsFailure)
        {
            logger.LogWarning("AI service failed for conversation {ConversationId}: {Error}", request.ConversationId, aiResult.Error);
        }

        var assistantMessage = new Message
        {
            Id = Guid.CreateVersion7(),
            ConversationId = request.ConversationId,
            Role = MessageRole.Assistant,
            Content = assistantContent,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _ = await repository.AddMessageAsync(assistantMessage, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Message processed for conversation {ConversationId}", request.ConversationId);

        return Result<MessageDto>.Success(new MessageDto(
            assistantMessage.Id,
            MapRole(assistantMessage.Role),
            assistantMessage.Content,
            assistantMessage.CreatedAt));
    }

    private static string MapRole(MessageRole role) => role switch
    {
        MessageRole.User => "user",
        MessageRole.Assistant => "assistant",
        _ => "unknown",
    };
}
