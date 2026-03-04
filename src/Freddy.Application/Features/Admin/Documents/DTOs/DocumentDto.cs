using Freddy.Application.Entities;

namespace Freddy.Application.Features.Admin.Documents.DTOs;

/// <summary>
/// Represents a document in admin API responses.
/// </summary>
public sealed record DocumentDto(
    Guid Id,
    Guid PackageId,
    string Name,
    string? Description,
    string Type,
    string? StepsContent,
    string? FileUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
