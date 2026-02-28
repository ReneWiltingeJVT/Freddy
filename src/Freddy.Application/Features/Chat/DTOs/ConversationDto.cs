namespace Freddy.Application.Features.Chat.DTOs;

public sealed record ConversationDto(Guid Id, string Title, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
