using Freddy.Application.Common;
using Freddy.Application.Common.Interfaces;
using Freddy.Application.Entities;
using Freddy.Application.Features.Chat.DTOs;
using MediatR;

namespace Freddy.Application.Features.Chat.Queries;

public sealed class GetConversationsQueryHandler(
    IConversationRepository repository,
    ICurrentUserService currentUser) : IRequestHandler<GetConversationsQuery, Result<IReadOnlyList<ConversationDto>>>
{
    public async Task<Result<IReadOnlyList<ConversationDto>>> Handle(
        GetConversationsQuery request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Conversation> conversations = await repository.GetByUserIdAsync(currentUser.UserId, cancellationToken).ConfigureAwait(false);

        var dtos = conversations
            .Select(c => new ConversationDto(c.Id, c.Title, c.CreatedAt, c.UpdatedAt))
            .ToList();

        return Result<IReadOnlyList<ConversationDto>>.Success(dtos);
    }
}
