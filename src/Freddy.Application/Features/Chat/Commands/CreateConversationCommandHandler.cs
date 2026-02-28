using Freddy.Application.Common;
using Freddy.Application.Common.Interfaces;
using Freddy.Application.Entities;
using Freddy.Application.Features.Chat.DTOs;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Freddy.Application.Features.Chat.Commands;

public sealed class CreateConversationCommandHandler(
    IConversationRepository repository,
    ICurrentUserService currentUser,
    ILogger<CreateConversationCommandHandler> logger) : IRequestHandler<CreateConversationCommand, Result<ConversationDto>>
{
    public async Task<Result<ConversationDto>> Handle(
        CreateConversationCommand request,
        CancellationToken cancellationToken)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var conversation = new Conversation
        {
            Id = Guid.CreateVersion7(),
            UserId = currentUser.UserId,
            Title = string.IsNullOrWhiteSpace(request.Title) ? "Nieuw gesprek" : request.Title.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };

        _ = await repository.CreateAsync(conversation, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Conversation {ConversationId} created for user {UserId}", conversation.Id, currentUser.UserId);

        return Result<ConversationDto>.Success(
            new ConversationDto(conversation.Id, conversation.Title, conversation.CreatedAt, conversation.UpdatedAt));
    }
}
