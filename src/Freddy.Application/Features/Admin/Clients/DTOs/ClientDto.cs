namespace Freddy.Application.Features.Admin.Clients.DTOs;

/// <summary>
/// Represents a client in admin API responses.
/// </summary>
public sealed record ClientDto(
    Guid Id,
    string DisplayName,
    IReadOnlyList<string> Aliases,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
