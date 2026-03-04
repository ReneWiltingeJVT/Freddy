using Freddy.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Freddy.Infrastructure.AI;

/// <summary>
/// Two-lane composite router that orchestrates fast-path and slow-path (LLM) routing.
///
/// Decision flow:
///   1. Fast-path scores all candidates deterministically (&lt;10ms)
///   2. If single candidate scores ≥HighConfidence → return directly
///   3. If single candidate scores ≥AmbiguityFloor → return with NeedsConfirmation
///   4. If 2+ candidates score ≥AmbiguityFloor → delegate to Ollama for disambiguation
///   5. If no candidate scores ≥AmbiguityFloor → return no-match fallback
///
/// This avoids calling Ollama for 80%+ of requests (clear matches or clear non-matches).
/// </summary>
public sealed class CompositePackageRouter(
    IFastPathRouter fastPathRouter,
    OllamaPackageRouter ollamaRouter,
    IOptions<RoutingOptions> options,
    ILogger<CompositePackageRouter> logger) : IPackageRouter
{
    public async Task<PackageRouterResult> RouteAsync(
        string userMessage,
        IReadOnlyList<PackageCandidate> candidates,
        CancellationToken cancellationToken)
    {
        if (candidates.Count == 0)
        {
            logger.LogInformation("[Routing] No candidates available");
            return CreateNoMatchResult("Geen pakketten beschikbaar.");
        }

        RoutingOptions config = options.Value;

        // Lane A: Fast-path deterministic scoring
        IReadOnlyList<ScoredCandidate> scored = fastPathRouter.Score(userMessage, candidates);

        if (scored.Count == 0)
        {
            logger.LogInformation("[Routing] Fast-path: no candidates scored above 0 → no match");
            return CreateNoMatchResult("Geen passend pakket gevonden.");
        }

        ScoredCandidate top = scored[0];

        // Case 1: Clear high-confidence match — return directly
        if (top.Score >= config.HighConfidenceThreshold)
        {
            return HandleHighConfidence(top);
        }

        // Evaluate ambiguity zone
        return await HandleAmbiguityZoneAsync(
            userMessage, scored, top, config, cancellationToken).ConfigureAwait(false);
    }

    private PackageRouterResult HandleHighConfidence(ScoredCandidate top)
    {
        logger.LogInformation(
            "[Routing] Fast-path: HIGH confidence match → {Package} (score={Score:F2})",
            top.Candidate.Title, top.Score);

        return new PackageRouterResult
        {
            ChosenPackageId = top.Candidate.Id,
            Confidence = top.Score,
            NeedsConfirmation = false,
            Reason = $"Directe match op basis van trefwoorden: {top.Candidate.Title}",
        };
    }

    private async Task<PackageRouterResult> HandleAmbiguityZoneAsync(
        string userMessage,
        IReadOnlyList<ScoredCandidate> scored,
        ScoredCandidate top,
        RoutingOptions config,
        CancellationToken cancellationToken)
    {
        var ambiguousCandidates = scored
            .Where(s => s.Score >= config.AmbiguityFloorThreshold)
            .ToList();

        // Single candidate in ambiguity zone — return with confirmation
        if (ambiguousCandidates.Count == 1)
        {
            logger.LogInformation(
                "[Routing] Fast-path: single medium-confidence match → {Package} (score={Score:F2}), asking confirmation",
                top.Candidate.Title, top.Score);

            return new PackageRouterResult
            {
                ChosenPackageId = top.Candidate.Id,
                Confidence = top.Score,
                NeedsConfirmation = true,
                Reason = $"Mogelijke match: {top.Candidate.Title}, bevestiging nodig.",
            };
        }

        // Multiple candidates in ambiguity zone → delegate to Ollama
        if (ambiguousCandidates.Count >= 2)
        {
            logger.LogInformation(
                "[Routing] Fast-path: {Count} ambiguous candidates (scores: {Scores}) → delegating to Ollama",
                ambiguousCandidates.Count,
                string.Join(", ", ambiguousCandidates.Select(s => $"{s.Candidate.Title}={s.Score:F2}")));

            PackageCandidate[] narrowedCandidates = [.. ambiguousCandidates.Select(s => s.Candidate)];

            return await ollamaRouter.RouteAsync(userMessage, narrowedCandidates, cancellationToken)
                .ConfigureAwait(false);
        }

        // No candidates above ambiguity floor
        logger.LogInformation(
            "[Routing] Fast-path: best score {Score:F2} below ambiguity floor {Floor:F2} → no match",
            top.Score, config.AmbiguityFloorThreshold);

        return CreateNoMatchResult("Geen passend pakket gevonden voor deze vraag.");
    }

    private static PackageRouterResult CreateNoMatchResult(string reason) => new()
    {
        ChosenPackageId = null,
        Confidence = 0.0,
        NeedsConfirmation = false,
        Reason = reason,
    };
}
