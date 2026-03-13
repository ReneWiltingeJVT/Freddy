namespace Freddy.Application.Common.Interfaces;

/// <summary>
/// Pre-built knowledge context containing summaries for injection into the LLM system prompt.
/// </summary>
/// <param name="PackageSummaries">Formatted summary of all published packages (title, description, category).</param>
/// <param name="ClientInfo">Formatted list of known client names.</param>
/// <param name="PersonalPlans">Formatted list of personal plans for the active client (empty when no client context).</param>
public sealed record KnowledgeContext(
    string PackageSummaries,
    string ClientInfo,
    string PersonalPlans)
{
    /// <summary>Returns the total character count of all context sections.</summary>
    public int TotalLength => PackageSummaries.Length + ClientInfo.Length + PersonalPlans.Length;
}
