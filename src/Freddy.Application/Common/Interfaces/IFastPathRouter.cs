namespace Freddy.Application.Common.Interfaces;

/// <summary>
/// Fast-path deterministic router that scores candidates using tag/synonym/title matching.
/// Returns scored candidates without calling the LLM.
/// </summary>
public interface IFastPathRouter
{
    /// <summary>
    /// Scores all candidates against the user message using deterministic keyword matching.
    /// Returns candidates with their scores, ordered by score descending. Never calls an LLM.
    /// </summary>
    IReadOnlyList<ScoredCandidate> Score(string userMessage, IReadOnlyList<PackageCandidate> candidates);
}
