using Freddy.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Freddy.Infrastructure.AI;

/// <summary>
/// Deterministic fast-path router that scores candidates using keyword matching
/// against titles, tags, synonyms, content keywords, and document names.
/// Runs in &lt;10ms — no LLM calls.
///
/// Scoring algorithm (per candidate, highest match wins):
///   1.0  — Exact title match (case-insensitive)
///   0.7  — Title contained in message
///   0.6  — Exact tag match
///   0.6  — Exact synonym match
///   0.5  — Document name match
///   0.4  — Content keyword overlap (≥3 shared words after stopword filtering)
///   0.35 — N-gram overlap (bigram similarity ≥0.3)
///   0.3  — Partial tag/synonym overlap (≥4 chars)
///   0.3  — Description word overlap (≥2 shared words, raised from 0.2)
/// </summary>
public sealed class FastPathRouter(ILogger<FastPathRouter> logger) : IFastPathRouter
{
    private const int MinPartialMatchLength = 4;
    private const int MinDescriptionOverlapWords = 2;
    private const int MinContentOverlapWords = 3;
    private const double NgramSimilarityThreshold = 0.3;
    private const double CategoryBoost = 0.1;

    private static readonly char[] WordSeparators = [' ', ',', '.', '!', '?', ';', ':', '-', '/', '(', ')', '\n', '\r', '\t'];

    /// <summary>
    /// Common Dutch stopwords filtered before content keyword scoring and n-gram matching.
    /// </summary>
    private static readonly HashSet<string> DutchStopwords = new(StringComparer.OrdinalIgnoreCase)
    {
        "de", "het", "een", "van", "en", "in", "is", "dat", "die", "op",
        "te", "aan", "met", "er", "als", "zijn", "voor", "was", "niet",
        "maar", "ook", "nog", "bij", "naar", "dan", "om", "uit", "tot",
        "kan", "wel", "wat", "ze", "dit", "hun", "worden", "meer", "hoe",
        "al", "zou", "over", "door", "geen", "veel", "moet", "waar",
        "wordt", "heeft", "deze", "ik", "je", "we", "wij", "zij", "hij",
        "mij", "jij", "haar", "hem", "ons", "hen", "wie", "men",
    };

    public IReadOnlyList<ScoredCandidate> Score(string userMessage, IReadOnlyList<PackageCandidate> candidates)
    {
        if (candidates.Count == 0)
        {
            return [];
        }

        string normalizedMessage = Normalize(userMessage);
        string[] allMessageWords = normalizedMessage.Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries);
        string[] filteredMessageWords = FilterStopwords(allMessageWords);

        List<ScoredCandidate> scored = new(candidates.Count);

        foreach (PackageCandidate candidate in candidates)
        {
            double score = ScoreCandidate(normalizedMessage, allMessageWords, filteredMessageWords, candidate);

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

    private static double ScoreCandidate(
        string normalizedMessage,
        string[] allMessageWords,
        string[] filteredMessageWords,
        PackageCandidate candidate)
    {
        double highestScore = ScoreTitle(normalizedMessage, candidate);
        highestScore = ScoreKeywords(normalizedMessage, allMessageWords, candidate.Tags, highestScore);
        highestScore = ScoreKeywords(normalizedMessage, allMessageWords, candidate.Synonyms, highestScore);
        highestScore = ScoreDocumentNames(normalizedMessage, allMessageWords, candidate.DocumentNames, highestScore);
        highestScore = ScoreContentKeywords(filteredMessageWords, candidate.Content, highestScore);
        highestScore = ScoreNgram(filteredMessageWords, candidate, highestScore);

        // Always score description — resolves cases where title/tag/synonym don't contain the query word
        // but description clearly describes the package (e.g. "voedselbank" in description).
        highestScore = ScoreDescription(filteredMessageWords, candidate.Description, highestScore);

        // Category boost: PersonalPlan packages get a small bonus to surface personal plans
        // when there's any match, making them easier to find during scoped retrieval.
        if (highestScore > 0 && candidate.Category == Application.Entities.PackageCategory.PersonalPlan)
        {
            highestScore = Math.Min(1.0, highestScore + CategoryBoost);
        }

        return highestScore;
    }

    private static double ScoreTitle(string normalizedMessage, PackageCandidate candidate)
    {
        string normalizedTitle = Normalize(candidate.Title);

        return string.Equals(normalizedMessage, normalizedTitle, StringComparison.Ordinal)
            ? 1.0
            : normalizedTitle.Length > 3 && normalizedMessage.Contains(normalizedTitle, StringComparison.Ordinal)
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

    /// <summary>
    /// Scores based on document names associated with the package (0.5).
    /// If a document name contains a message word (≥4 chars) or vice versa, it scores.
    /// </summary>
    private static double ScoreDocumentNames(
        string normalizedMessage,
        string[] messageWords,
        IReadOnlyList<string>? documentNames,
        double currentHighest)
    {
        if (currentHighest >= 0.5 || documentNames is null || documentNames.Count == 0)
        {
            return currentHighest;
        }

        foreach (string docName in documentNames)
        {
            string normalizedDocName = Normalize(docName);
            if (string.IsNullOrEmpty(normalizedDocName))
            {
                continue;
            }

            // Full document name in message or message contains document name
            if (normalizedMessage.Contains(normalizedDocName, StringComparison.Ordinal))
            {
                return Math.Max(currentHighest, 0.5);
            }

            // Individual words from document name in message (≥4 chars)
            string[] docWords = normalizedDocName.Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries);
            string[] filteredDocWords = FilterStopwords(docWords);

            foreach (string docWord in filteredDocWords)
            {
                if (docWord.Length >= MinPartialMatchLength
                    && messageWords.Any(mw => mw.Length >= MinPartialMatchLength
                        && (mw.Contains(docWord, StringComparison.Ordinal) || docWord.Contains(mw, StringComparison.Ordinal))))
                {
                    return Math.Max(currentHighest, 0.5);
                }
            }
        }

        return currentHighest;
    }

    /// <summary>
    /// Scores based on content keyword overlap (0.4).
    /// After stopword filtering, ≥3 overlapping words between message and content.
    /// </summary>
    private static double ScoreContentKeywords(
        string[] filteredMessageWords,
        string content,
        double currentHighest)
    {
        if (currentHighest >= 0.4 || string.IsNullOrWhiteSpace(content))
        {
            return currentHighest;
        }

        string[] contentWords = Normalize(content).Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries);
        string[] filteredContentWords = FilterStopwords(contentWords);

        HashSet<string> contentWordSet = new(filteredContentWords, StringComparer.Ordinal);

        int overlapCount = filteredMessageWords.Count(mw =>
            mw.Length >= MinPartialMatchLength && contentWordSet.Contains(mw));

        return overlapCount >= MinContentOverlapWords
            ? Math.Max(currentHighest, 0.4)
            : currentHighest;
    }

    /// <summary>
    /// Scores using bigram similarity for compound Dutch words (0.35).
    /// Compares bigrams of each filtered message word against tag/synonym/title words.
    /// </summary>
    private static double ScoreNgram(
        string[] filteredMessageWords,
        PackageCandidate candidate,
        double currentHighest)
    {
        if (currentHighest >= 0.35)
        {
            return currentHighest;
        }

        // Collect all significant candidate words (from tags, synonyms, title)
        List<string> candidateWords = [];
        foreach (string tag in candidate.Tags)
        {
            candidateWords.Add(Normalize(tag));
        }

        foreach (string synonym in candidate.Synonyms)
        {
            candidateWords.Add(Normalize(synonym));
        }

        string[] titleWords = Normalize(candidate.Title)
            .Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries);
        candidateWords.AddRange(titleWords.Where(w => w.Length >= MinPartialMatchLength));

        foreach (string messageWord in filteredMessageWords)
        {
            if (messageWord.Length < MinPartialMatchLength)
            {
                continue;
            }

            HashSet<string> messageBigrams = GetBigrams(messageWord);

            foreach (string candidateWord in candidateWords)
            {
                if (candidateWord.Length < MinPartialMatchLength)
                {
                    continue;
                }

                HashSet<string> candidateBigrams = GetBigrams(candidateWord);

                double similarity = BigramSimilarity(messageBigrams, candidateBigrams);
                if (similarity >= NgramSimilarityThreshold)
                {
                    return Math.Max(currentHighest, 0.35);
                }
            }
        }

        return currentHighest;
    }

    private static double ScoreDescription(string[] filteredMessageWords, string description, double currentHighest)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return currentHighest;
        }

        string[] descriptionWords = FilterStopwords(
            Normalize(description).Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries));

        int overlapCount = filteredMessageWords.Count(mw =>
            mw.Length >= MinPartialMatchLength
            && descriptionWords.Any(dw => string.Equals(dw, mw, StringComparison.Ordinal)));

        // 4+ matching words → confident description match (0.5 = high-confidence threshold)
        // 2–3 matching words → plausible description match (0.4 = above ambiguity floor)
        // <2 → no description score
        double score = overlapCount >= 4 ? 0.5
            : overlapCount >= MinDescriptionOverlapWords ? 0.4
            : 0.0;

        return Math.Max(currentHighest, score);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static string Normalize(string input) =>
        input.Trim().ToLowerInvariant();

    private static string[] FilterStopwords(string[] words) =>
        [.. words.Where(w => !DutchStopwords.Contains(w))];

    /// <summary>
    /// Generates character bigrams for a word (e.g., "medicatie" → {"me","ed","di","ic","ca","at","ti","ie"}).
    /// </summary>
    internal static HashSet<string> GetBigrams(string word)
    {
        if (word.Length < 2)
        {
            return [];
        }

        HashSet<string> bigrams = new(word.Length - 1, StringComparer.Ordinal);
        for (int i = 0; i < word.Length - 1; i++)
        {
            _ = bigrams.Add(word.Substring(i, 2));
        }

        return bigrams;
    }

    /// <summary>
    /// Calculates the Dice coefficient between two bigram sets:
    /// 2 * |intersection| / (|set1| + |set2|).
    /// </summary>
    internal static double BigramSimilarity(HashSet<string> set1, HashSet<string> set2)
    {
        if (set1.Count == 0 || set2.Count == 0)
        {
            return 0.0;
        }

        int intersection = set1.Count(set2.Contains);
        return 2.0 * intersection / (set1.Count + set2.Count);
    }
}
