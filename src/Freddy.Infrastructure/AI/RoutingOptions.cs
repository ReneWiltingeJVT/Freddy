namespace Freddy.Infrastructure.AI;

/// <summary>
/// Configuration for the two-lane routing strategy.
/// Bound to the "Routing" section in appsettings.json.
/// </summary>
public sealed class RoutingOptions
{
    public const string SectionName = "Routing";

    /// <summary>
    /// Fast-path score at or above which a single candidate is returned directly (no LLM, no confirmation).
    /// Default: 0.6
    /// </summary>
    public double HighConfidenceThreshold { get; set; } = 0.6;

    /// <summary>
    /// Minimum fast-path score for a candidate to be considered in the ambiguity zone.
    /// Candidates below this are treated as non-matches.
    /// Default: 0.3
    /// </summary>
    public double AmbiguityFloorThreshold { get; set; } = 0.3;
}
