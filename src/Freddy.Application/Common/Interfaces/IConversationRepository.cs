using Freddy.Application.Entities;

namespace Freddy.Application.Common.Interfaces;

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Conversation>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<Conversation> CreateAsync(Conversation conversation, CancellationToken cancellationToken);

    Task<Message> AddMessageAsync(Message message, CancellationToken cancellationToken);

    Task<IReadOnlyList<Message>> GetMessagesAsync(Guid conversationId, CancellationToken cancellationToken);

    Task DeleteAsync(Guid conversationId, CancellationToken cancellationToken);

    Task SetPendingStateAsync(
        Guid conversationId,
        Guid? packageId,
        ConversationPendingState state,
        CancellationToken cancellationToken);

    /// <summary>
    /// Persists the detected client ID on the conversation so subsequent turns
    /// can use client context without re-detecting the client from the message.
    /// Pass null to clear the pending client.
    /// </summary>
    Task SetPendingClientIdAsync(
        Guid conversationId,
        Guid? clientId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns the most recent <paramref name="count"/> messages for a conversation,
    /// ordered chronologically (oldest first). Used to build LLM conversation history.
    /// </summary>
    Task<IReadOnlyList<Message>> GetRecentMessagesAsync(
        Guid conversationId,
        int count,
        CancellationToken cancellationToken);
}
