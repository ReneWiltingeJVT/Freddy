namespace Freddy.Application.Common.Interfaces;

/// <summary>
/// Represents a candidate package that the router can choose from.
/// </summary>
public sealed record PackageCandidate(Guid Id, string Title, string Description);
