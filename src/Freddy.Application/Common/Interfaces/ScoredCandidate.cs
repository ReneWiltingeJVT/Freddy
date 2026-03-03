namespace Freddy.Application.Common.Interfaces;

/// <summary>
/// A package candidate with a deterministic relevance score from the fast-path router.
/// </summary>
public sealed record ScoredCandidate(PackageCandidate Candidate, double Score);
