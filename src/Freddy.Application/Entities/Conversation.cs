namespace Freddy.Application.Entities;

public sealed class Conversation
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Set when the assistant is waiting for user input (package confirmation or document delivery).
    /// Cleared once the interaction resolves.
    /// </summary>
    public Guid? PendingPackageId { get; set; }

    /// <summary>
    /// Tracks what the assistant is currently waiting for.
    /// </summary>
    public ConversationPendingState PendingState { get; set; } = ConversationPendingState.None;

    public ICollection<Message> Messages { get; set; } = [];
}
