using Freddy.Application.Common;
using Freddy.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Freddy.Application.Features.Chat.Commands;

public sealed class DeleteConversationCommandHandler(
    IConversationRepository conversationRepository,
    ILogger<DeleteConversationCommandHandler> logger) : IRequestHandler<DeleteConversationCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteConversationCommand request, CancellationToken cancellationToken)
    {
        Entities.Conversation? conversation = await conversationRepository
            .GetByIdAsync(request.ConversationId, cancellationToken)
            .ConfigureAwait(false);

        if (conversation is null)
        {
            return Result<bool>.NotFound($"Conversation {request.ConversationId} not found.");
        }

        await conversationRepository
            .DeleteAsync(request.ConversationId, cancellationToken)
            .ConfigureAwait(false);

        logger.LogInformation("Deleted conversation {ConversationId}", request.ConversationId);

        return Result<bool>.Success(value: true);
    }
}
