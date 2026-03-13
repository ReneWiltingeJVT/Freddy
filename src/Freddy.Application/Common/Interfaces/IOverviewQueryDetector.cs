using Freddy.Application.Entities;

namespace Freddy.Application.Common.Interfaces;

/// <summary>
/// Types of overview queries that Freddy can answer without full routing.
/// </summary>
public enum OverviewQueryType
{
    /// <summary>Not an overview query — fall through to normal routing.</summary>
    None,

    /// <summary>"Hoeveel protocollen zijn er?" — count packages of a given category.</summary>
    CountByCategory,

    /// <summary>"Welke werkinstructies zijn er?" — list packages of a given category.</summary>
    ListByCategory,

    /// <summary>"Welke plannen zijn er voor meneer X?" — list personal plans for a client.</summary>
    PersonalPlansForClient,

    /// <summary>"Welke pakketten zijn er?" — list all published packages.</summary>
    ListAll,
}

/// <summary>
/// Parsed result of an overview query detection.
/// </summary>
public sealed record OverviewQueryIntent
{
    public OverviewQueryType QueryType { get; init; }

    /// <summary>Target category for CountByCategory / ListByCategory / PersonalPlansForClient.</summary>
    public PackageCategory? Category { get; init; }

    /// <summary>Name fragment extracted from the user message to help identify the client.</summary>
    public string? ClientNameHint { get; init; }

    /// <summary>True when this intent requires special handling outside the normal routing pipeline.</summary>
    public bool IsOverview => QueryType != OverviewQueryType.None;

    public static readonly OverviewQueryIntent None = new() { QueryType = OverviewQueryType.None };
}

/// <summary>
/// Detects whether a user message is an overview/count/list question about packages or clients.
/// All detection is deterministic — no LLM calls.
/// </summary>
public interface IOverviewQueryDetector
{
    OverviewQueryIntent Detect(string message);
}
