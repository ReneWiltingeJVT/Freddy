namespace Freddy.Application.Features.Chat.DTOs;

public sealed record MessageDto(
    Guid Id,
    string Role,
    string Content,
    DateTimeOffset CreatedAt,
    IReadOnlyList<AttachmentDto>? Attachments = null);
