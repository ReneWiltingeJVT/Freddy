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
///   5. If no candidate scores ≥AmbiguityFloor → LLM zero-match recovery
///   6. If LLM zero-match also fails → return top-3 suggestions for user guidance
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
            logger.LogInformation("[Routing] Fast-path: no candidates scored above 0 → trying LLM zero-match recovery");
            return await HandleZeroMatchRecoveryAsync(userMessage, candidates, scored, cancellationToken)
                .ConfigureAwait(false);
        }

        ScoredCandidate top = scored[0];

        // Case 1: Clear high-confidence match — return directly
        if (top.Score >= config.HighConfidenceThreshold)
        {
            return HandleHighConfidence(top);
        }

        // Evaluate ambiguity zone
        return await HandleAmbiguityZoneAsync(
            userMessage, scored, top, candidates, config, cancellationToken).ConfigureAwait(false);
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
        IReadOnlyList<PackageCandidate> allCandidates,
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

        // No candidates above ambiguity floor → LLM zero-match recovery
        logger.LogInformation(
            "[Routing] Fast-path: best score {Score:F2} below ambiguity floor {Floor:F2} → trying LLM zero-match recovery",
            top.Score, config.AmbiguityFloorThreshold);

        return await HandleZeroMatchRecoveryAsync(userMessage, allCandidates, scored, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// LLM zero-match recovery: when FastPath finds no usable matches, the LLM tries to
    /// find a semantic match by analysing all candidates. If LLM also fails, returns the
    /// top-3 scored packages as suggestions.
    /// </summary>
    private async Task<PackageRouterResult> HandleZeroMatchRecoveryAsync(
        string userMessage,
        IReadOnlyList<PackageCandidate> allCandidates,
        IReadOnlyList<ScoredCandidate> scored,
        CancellationToken cancellationToken)
    {
        try
        {
            PackageRouterResult llmResult = await ollamaRouter
                .RouteAsync(userMessage, allCandidates, cancellationToken)
                .ConfigureAwait(false);

            if (llmResult.IsSuccessful)
            {
                logger.LogInformation(
                    "[Routing] LLM zero-match recovery: found match {PackageId} (confidence={Confidence:F2})",
                    llmResult.ChosenPackageId, llmResult.Confidence);
                return llmResult;
            }

            if (llmResult.IsServiceUnavailable)
            {
                return llmResult;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "[Routing] LLM zero-match recovery failed, falling back to suggestions");
        }

        // Both FastPath and LLM failed — return suggestions from top FastPath scores
        return CreateSuggestionResult(scored);
    }

    /// <summary>
    /// Builds a result with the top-3 weakly scored packages as suggestions.
    /// </summary>
    private PackageRouterResult CreateSuggestionResult(IReadOnlyList<ScoredCandidate> scored)
    {
        IReadOnlyList<ScoredCandidate> top3 = scored.Take(3).ToList();

        if (top3.Count == 0)
        {
            logger.LogInformation("[Routing] No suggestions available — no candidates scored above 0");
            return CreateNoMatchResult("Geen passend pakket gevonden.");
        }

        string suggestions = string.Join(", ", top3.Select(s => s.Candidate.Title));
        logger.LogInformation("[Routing] Returning {Count} suggestions: {Suggestions}", top3.Count, suggestions);

        return new PackageRouterResult
        {
            ChosenPackageId = null,
            Confidence = 0.0,
            NeedsConfirmation = false,
            Reason = "Geen directe match gevonden.",
            SuggestedPackages = top3.Select(s => new SuggestedPackage(s.Candidate.Id, s.Candidate.Title, s.Candidate.Description)).ToList(),
        };
    }

    private static PackageRouterResult CreateNoMatchResult(string reason) => new()
    {
        ChosenPackageId = null,
        Confidence = 0.0,
        NeedsConfirmation = false,
        Reason = reason,
    };
}
