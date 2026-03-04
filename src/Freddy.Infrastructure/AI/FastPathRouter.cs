using Freddy.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Freddy.Infrastructure.AI;

/// <summary>
/// Deterministic fast-path router that scores candidates using keyword matching
/// against titles, tags, and synonyms. Runs in &lt;10ms — no LLM calls.
///
/// Scoring algorithm (per candidate, highest match wins):
///   1.0  — Exact title match (case-insensitive)
///   0.7  — Title contained in message
///   0.6  — Exact tag match
///   0.6  — Exact synonym match
///   0.3  — Partial tag/synonym overlap (≥4 chars)
///   0.2  — Description word overlap (≥2 shared words)
/// </summary>
public sealed class FastPathRouter(ILogger<FastPathRouter> logger) : IFastPathRouter
{
    private const int MinPartialMatchLength = 4;
    private const int MinDescriptionOverlapWords = 2;

    private static readonly char[] WordSeparators = [' ', ',', '.', '!', '?', ';', ':', '-', '/', '(', ')', '\n', '\r', '\t'];

    public IReadOnlyList<ScoredCandidate> Score(string userMessage, IReadOnlyList<PackageCandidate> candidates)
    {
        if (candidates.Count == 0)
        {
            return [];
        }

        string normalizedMessage = Normalize(userMessage);
        string[] messageWords = normalizedMessage.Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries);

        List<ScoredCandidate> scored = new(candidates.Count);

        foreach (PackageCandidate candidate in candidates)
        {
            double score = ScoreCandidate(normalizedMessage, messageWords, candidate);

            if (score > 0)
            {
                scored.Add(new ScoredCandidate(candidate, score));
            }
        }

        scored.Sort((a, b) => b.Score.CompareTo(a.Score));

        logger.LogInformation(
            "[FastPath] Scored {CandidateCount} candidates for message '{Message}': {Results}",
            candidates.Count,
            userMessage.Length > 80 ? userMessage[..80] + "..." : userMessage,
            string.Join(", ", scored.Select(s => $"{s.Candidate.Title}={s.Score:F2}")));

        return scored;
    }

    private static double ScoreCandidate(string normalizedMessage, string[] messageWords, PackageCandidate candidate)
    {
        double highestScore = ScoreTitle(normalizedMessage, candidate);
        highestScore = ScoreKeywords(normalizedMessage, messageWords, candidate.Tags, highestScore);
        highestScore = ScoreKeywords(normalizedMessage, messageWords, candidate.Synonyms, highestScore);

        if (highestScore < 0.2)
        {
            highestScore = ScoreDescription(messageWords, candidate.Description, highestScore);
        }

        return highestScore;
    }

    private static double ScoreTitle(string normalizedMessage, PackageCandidate candidate)
    {
        string normalizedTitle = Normalize(candidate.Title);

        if (string.Equals(normalizedMessage, normalizedTitle, StringComparison.Ordinal))
        {
            return 1.0;
        }

        return normalizedTitle.Length > 3 && normalizedMessage.Contains(normalizedTitle, StringComparison.Ordinal)
            ? 0.7
            : 0.0;
    }

    private static double ScoreKeywords(
        string normalizedMessage,
        string[] messageWords,
        IReadOnlyList<string> keywords,
        double currentHighest)
    {
        double highest = currentHighest;

        foreach (string keyword in keywords)
        {
            string normalized = Normalize(keyword);
            if (string.IsNullOrEmpty(normalized))
            {
                continue;
            }

            if (normalizedMessage.Contains(normalized, StringComparison.Ordinal))
            {
                highest = Math.Max(highest, 0.6);
            }
            else if (normalized.Length >= MinPartialMatchLength
                && messageWords.Any(w => w.Length >= MinPartialMatchLength
                    && (w.Contains(normalized, StringComparison.Ordinal) || normalized.Contains(w, StringComparison.Ordinal))))
            {
                highest = Math.Max(highest, 0.3);
            }
        }

        return highest;
    }

    private static double ScoreDescription(string[] messageWords, string description, double currentHighest)
    {
        string[] descriptionWords = Normalize(description)
            .Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries);

        int overlapCount = messageWords.Count(mw =>
            mw.Length >= MinPartialMatchLength
            && descriptionWords.Any(dw => string.Equals(dw, mw, StringComparison.Ordinal)));

        return overlapCount >= MinDescriptionOverlapWords
            ? Math.Max(currentHighest, 0.2)
            : currentHighest;
    }

    private static string Normalize(string input) =>
        input.Trim().ToLowerInvariant();
}
