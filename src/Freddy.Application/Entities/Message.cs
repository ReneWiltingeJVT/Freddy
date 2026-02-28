namespace Freddy.Application.Entities;

public sealed class Message
{
    public Guid Id { get; set; }

    public Guid ConversationId { get; set; }

    public MessageRole Role { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public Conversation Conversation { get; set; } = null!;
}
