using Freddy.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Freddy.Infrastructure.AI;

/// <summary>
/// Deterministic small talk detector using curated Dutch word/phrase lists.
/// Runs in &lt;1ms — no LLM calls, no external dependencies.
///
/// Detection priority:
///   1. Exact phrase match (full message matches a known phrase)
///   2. Prefix match (message starts with a known greeting/word)
///   3. Punctuation-only pattern (e.g. "???", "?!")
///   4. None — falls through to routing pipeline
///
/// All comparisons are case-insensitive after normalization (lowercase + trim).
/// </summary>
public sealed class SmallTalkDetector(ILogger<SmallTalkDetector> logger) : ISmallTalkDetector
{
    // ── Templates ──────────────────────────────────────────────────────
    private static class Templates
    {
        public const string Greeting = "Hoi! 👋 Waarmee kan ik je helpen?";
        public const string HelpIntent = "Natuurlijk! Waar gaat je vraag over?";
        public const string Thanks = "Graag gedaan! Heb je nog een andere vraag?";
        public const string Farewell = "Tot ziens! Als je nog vragen hebt, ben ik er. 👋";
        public const string GenericConfusion = "Geen probleem! Probeer je vraag anders te stellen, bijvoorbeeld: 'Hoe vraag ik een voedselpakket aan?'";
    }

    // ── Exact phrases (full message must match one of these) ──────────

    private static readonly HashSet<string> GreetingPhrases = new(StringComparer.OrdinalIgnoreCase)
    {
        "hoi", "hallo", "hey", "hi", "hé", "he", "hee", "heey", "yo",
        "dag", "moin", "hallootjes", "hoi hoi",
        "goedemorgen", "goedemiddag", "goedeavond", "goedenacht",
        "goeiemorgen", "goeiendag", "goeiedag", "goedenavond",
        "hoi freddy", "hallo freddy", "hey freddy", "hi freddy",
        "hoi!", "hallo!", "hey!", "hi!",
    };

    private static readonly HashSet<string> HelpIntentPhrases = new(StringComparer.OrdinalIgnoreCase)
    {
        "ik heb een vraag", "ik heb een vraagje",
        "kun je me helpen", "kun je me helpen?", "kan je me helpen",
        "kan je me helpen?", "kun je mij helpen", "kun je mij helpen?",
        "help", "hulp", "help me", "help mij",
        "ik heb hulp nodig", "hulp nodig",
        "ik zoek iets", "ik wil iets weten", "ik wil iets vragen",
        "waar kan ik terecht", "waar kan ik terecht?",
        "hoe werkt dit", "hoe werkt dit?",
        "wat kan je", "wat kan je?", "wat kun je", "wat kun je?",
        "wat kan jij", "wat kan jij?",
    };

    private static readonly HashSet<string> ThanksPhrases = new(StringComparer.OrdinalIgnoreCase)
    {
        "dank je", "dank je!", "bedankt", "bedankt!",
        "dankjewel", "dankjewel!", "dank je wel", "dank je wel!",
        "dank u", "dank u!", "dank u wel", "dank u wel!",
        "thanks", "thanks!", "thanx", "thx",
        "hartelijk dank", "hartelijk bedankt",
        "top bedankt", "super bedankt", "fijn dank je",
        "merci", "merci!", "dankuwel",
        "top dankjewel", "super dankjewel",
        "oke bedankt", "oké bedankt", "ok bedankt",
        "top", "top!", "super", "super!",
    };

    private static readonly HashSet<string> FarewellPhrases = new(StringComparer.OrdinalIgnoreCase)
    {
        "doei", "doei!", "dag", "dag!",
        "doeg", "doeg!", "bye", "bye!",
        "tot ziens", "tot ziens!",
        "tot later", "tot later!",
        "tot de volgende keer", "tot de volgende keer!",
        "fijne dag", "fijne dag!",
        "fijne avond", "fijne avond!",
        "fijne dienst", "fijne dienst!",
        "succes", "succes!",
    };

    private static readonly HashSet<string> ConfusionPhrases = new(StringComparer.OrdinalIgnoreCase)
    {
        "huh", "huh?", "hè", "hè?", "he?",
        "wat", "wat?", "wat??",
        "ik snap het niet", "ik begrijp het niet",
        "snap ik niet", "begrijp ik niet",
        "ik snap er niks van", "ik snap er niets van",
        "wat bedoel je", "wat bedoel je?",
        "ik snap dit niet", "ik begrijp dit niet",
        "help ik snap het niet",
    };

    // ── Greeting prefixes (message starts with these, rest is short) ──

    private static readonly string[] GreetingPrefixes =
    [
        "hoi ", "hallo ", "hey ", "hi ", "goedemorgen ", "goedemiddag ",
        "goedeavond ", "goeiemorgen ", "goeiendag ", "goeiedag ",
    ];

    public SmallTalkResult Detect(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return SmallTalkResult.NoMatch;
        }

        string normalized = Normalize(message);

        if (string.IsNullOrEmpty(normalized))
        {
            return SmallTalkResult.NoMatch;
        }

        // Step 1: Punctuation-only patterns (???, ?!, etc.)
        if (IsPunctuationOnly(normalized))
        {
            logger.LogInformation("[SmallTalk] Detected: {Category} for message '{Message}'",
                SmallTalkCategory.GenericConfusion, TruncateForLog(message));
            return new SmallTalkResult(SmallTalkCategory.GenericConfusion, Templates.GenericConfusion);
        }

        // Step 2: Exact phrase match
        SmallTalkResult exactMatch = TryExactMatch(normalized);
        if (exactMatch.IsSmallTalk)
        {
            logger.LogInformation("[SmallTalk] Detected: {Category} for message '{Message}'",
                exactMatch.Category, TruncateForLog(message));
            return exactMatch;
        }

        // Step 3: Greeting prefix match (e.g. "hoi, ik heb een vraag" should NOT be small talk)
        // Only match if the rest after the prefix is very short (≤ 15 chars) to avoid
        // false positives on "hoi, hoe vraag ik een voedselpakket aan?"
        SmallTalkResult prefixMatch = TryGreetingPrefixMatch(normalized);
        if (prefixMatch.IsSmallTalk)
        {
            logger.LogInformation("[SmallTalk] Detected: {Category} (prefix) for message '{Message}'",
                prefixMatch.Category, TruncateForLog(message));
            return prefixMatch;
        }

        return SmallTalkResult.NoMatch;
    }

    private static SmallTalkResult TryExactMatch(string normalized)
    {
        if (GreetingPhrases.Contains(normalized))
        {
            return new SmallTalkResult(SmallTalkCategory.Greeting, Templates.Greeting);
        }

        if (ThanksPhrases.Contains(normalized))
        {
            return new SmallTalkResult(SmallTalkCategory.Thanks, Templates.Thanks);
        }

        return HelpIntentPhrases.Contains(normalized)
            ? new SmallTalkResult(SmallTalkCategory.HelpIntent, Templates.HelpIntent)
            : FarewellPhrases.Contains(normalized)
            ? new SmallTalkResult(SmallTalkCategory.Farewell, Templates.Farewell)
            : ConfusionPhrases.Contains(normalized)
            ? new SmallTalkResult(SmallTalkCategory.GenericConfusion, Templates.GenericConfusion)
            : SmallTalkResult.NoMatch;
    }

    private static SmallTalkResult TryGreetingPrefixMatch(string normalized)
    {
        foreach (string prefix in GreetingPrefixes)
        {
            if (normalized.StartsWith(prefix, StringComparison.Ordinal))
            {
                string remainder = normalized[prefix.Length..].Trim(' ', ',', '.', '!');

                // Short remainder means it's just a greeting variant (e.g. "hoi daar", "hallo zeg")
                // Long remainder means there's a real question attached — don't classify as small talk
                if (remainder.Length <= 15)
                {
                    return new SmallTalkResult(SmallTalkCategory.Greeting, Templates.Greeting);
                }
            }
        }

        return SmallTalkResult.NoMatch;
    }

    private static bool IsPunctuationOnly(string normalized) =>
        normalized.Length is > 0 and <= 10
            && normalized.All(c => c is '?' or '!' or '.' or ' ');

    private static string Normalize(string input) =>
        input.Trim().ToLowerInvariant();

    private static string TruncateForLog(string input) =>
        input.Length > 60 ? input[..60] + "..." : input;
}
