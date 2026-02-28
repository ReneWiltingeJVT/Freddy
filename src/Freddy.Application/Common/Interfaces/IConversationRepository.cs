using Freddy.Application.Entities;

namespace Freddy.Application.Common.Interfaces;

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Conversation>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<Conversation> CreateAsync(Conversation conversation, CancellationToken cancellationToken);

    Task<Message> AddMessageAsync(Message message, CancellationToken cancellationToken);

    Task<IReadOnlyList<Message>> GetMessagesAsync(Guid conversationId, CancellationToken cancellationToken);
}
