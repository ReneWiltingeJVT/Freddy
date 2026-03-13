using System.Text.RegularExpressions;
using Freddy.Application.Common.Interfaces;
using Freddy.Application.Entities;
using Microsoft.Extensions.Logging;

namespace Freddy.Infrastructure.AI;

/// <summary>
/// Deterministic overview query detector.
/// Recognises count/list questions about package categories and personal client plans.
/// Runs in &lt;1ms — no LLM calls.
///
/// Supported patterns (Dutch):
///   "hoeveel protocollen zijn er?"     → CountByCategory(Protocol)
///   "welke werkinstructies zijn er?"   → ListByCategory(WorkInstruction)
///   "welke plannen zijn er voor meneer van het Hout?" → PersonalPlansForClient
///   "hoeveel pakketten zijn er?"       → CountByCategory / ListAll
/// </summary>
public sealed partial class OverviewQueryDetector(ILogger<OverviewQueryDetector> logger) : IOverviewQueryDetector
{
    // ── Count/list intent ───────────────────────────────────────────────
    [GeneratedRegex(@"\b(hoeveel|aantal)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CountPattern();

    [GeneratedRegex(@"\b(welke|alle|overzicht|geef\s+me|toon|laat\s+zien|wat\s+zijn)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ListPattern();

    // ── Category recognition ────────────────────────────────────────────
    [GeneratedRegex(@"\b(protocol|protocollen|protocols)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ProtocolPattern();

    [GeneratedRegex(@"\b(werkinstructie|werkinstructies|instructie|instructies|werkinstructi\w+)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex WorkInstructionPattern();

    [GeneratedRegex(@"\b(persoonlijk\s*plan|persoonlijke\s*plannen|personal\s*plan|plannen?)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PersonalPlanPattern();

    [GeneratedRegex(@"\b(pakket|pakketten)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PackagePattern();

    // ── Client name extraction ──────────────────────────────────────────
    // Matches: "voor meneer van het Hout", "voor mevrouw Jansen", "van de heer Smith"
    [GeneratedRegex(
        @"\b(?:voor|van)\s+(?:meneer|mevrouw|de\s+heer|dhr\.|mw\.)\s+(.+?)(?:\?|\s*$)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ForNamedClientPattern();

    // Fallback: "voor [name]" without honorific — only match if name starts with capital
    [GeneratedRegex(
        @"\bvoor\s+([A-Z][a-zA-Z]+(?:\s+(?:de|van|den|der)\s+[A-Z][a-zA-Z]+|(?:\s+[A-Z][a-zA-Z]+))*)\b")]
    private static partial Regex ForCapitalizedClientPattern();

    public OverviewQueryIntent Detect(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return OverviewQueryIntent.None;
        }

        bool isCount = CountPattern().IsMatch(message);
        bool isList = ListPattern().IsMatch(message);

        if (!isCount && !isList)
        {
            return OverviewQueryIntent.None;
        }

        bool isProtocol = ProtocolPattern().IsMatch(message);
        bool isWorkInstruction = WorkInstructionPattern().IsMatch(message);
        bool isPersonalPlan = PersonalPlanPattern().IsMatch(message);
        bool isPackage = PackagePattern().IsMatch(message);

        // Personal plan + client name hint = client-scoped plan query
        if (isPersonalPlan)
        {
            string? clientHint = ExtractClientNameHint(message);
            if (clientHint is not null)
            {
                logger.LogDebug("[OverviewQuery] PersonalPlansForClient, hint='{Hint}'", clientHint);
                return new OverviewQueryIntent
                {
                    QueryType = OverviewQueryType.PersonalPlansForClient,
                    Category = PackageCategory.PersonalPlan,
                    ClientNameHint = clientHint,
                };
            }

            // No client named → list/count all personal plans
            OverviewQueryType planType = isCount ? OverviewQueryType.CountByCategory : OverviewQueryType.ListByCategory;
            logger.LogDebug("[OverviewQuery] {Type} PersonalPlan (no client hint)", planType);
            return new OverviewQueryIntent { QueryType = planType, Category = PackageCategory.PersonalPlan };
        }

        if (isProtocol)
        {
            OverviewQueryType t = isCount ? OverviewQueryType.CountByCategory : OverviewQueryType.ListByCategory;
            logger.LogDebug("[OverviewQuery] {Type} Protocol", t);
            return new OverviewQueryIntent { QueryType = t, Category = PackageCategory.Protocol };
        }

        if (isWorkInstruction)
        {
            OverviewQueryType t = isCount ? OverviewQueryType.CountByCategory : OverviewQueryType.ListByCategory;
            logger.LogDebug("[OverviewQuery] {Type} WorkInstruction", t);
            return new OverviewQueryIntent { QueryType = t, Category = PackageCategory.WorkInstruction };
        }

        if (isPackage || isList)
        {
            OverviewQueryType t = isCount ? OverviewQueryType.CountByCategory : OverviewQueryType.ListAll;
            logger.LogDebug("[OverviewQuery] {Type} (no specific category)", t);
            return new OverviewQueryIntent { QueryType = t };
        }

        return OverviewQueryIntent.None;
    }

    private string? ExtractClientNameHint(string message)
    {
        Match namedMatch = ForNamedClientPattern().Match(message);
        if (namedMatch.Success)
        {
            string hint = namedMatch.Groups[1].Value.Trim().TrimEnd('?', ' ');
            if (!string.IsNullOrWhiteSpace(hint))
            {
                return hint;
            }
        }

        Match capitalMatch = ForCapitalizedClientPattern().Match(message);
        if (capitalMatch.Success)
        {
            string hint = capitalMatch.Groups[1].Value.Trim();
            if (!string.IsNullOrWhiteSpace(hint))
            {
                return hint;
            }
        }

        return null;
    }
}
