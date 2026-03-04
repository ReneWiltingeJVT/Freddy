namespace Freddy.Application.Features.Chat.DTOs;

/// <summary>Represents a downloadable document attached to an assistant message.</summary>
public sealed record AttachmentDto(string Name, string Url, string? Description);
