using Freddy.Application.Entities;

namespace Freddy.Application.Common.Interfaces;

/// <summary>
/// Represents a candidate package that the router can choose from.
/// </summary>
public sealed record PackageCandidate(
    Guid Id,
    string Title,
    string Description,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Synonyms,
    string Content = "",
    IReadOnlyList<string>? DocumentNames = null,
    PackageCategory Category = PackageCategory.Protocol);
