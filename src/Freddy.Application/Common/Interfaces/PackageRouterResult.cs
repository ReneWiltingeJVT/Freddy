namespace Freddy.Application.Common.Interfaces;

/// <summary>
/// The structured result from the LLM router.
/// </summary>
public sealed record PackageRouterResult
{
    public Guid? ChosenPackageId { get; init; }

    public double Confidence { get; init; }

    public bool NeedsConfirmation { get; init; }

    public string? Reason { get; init; }

    /// <summary>
    /// Indicates the AI service was unreachable (Ollama down, timeout, etc.).
    /// </summary>
    public bool IsServiceUnavailable { get; init; }

    /// <summary>
    /// When no match is found (neither FastPath nor LLM), contains the top-3 weakly
    /// scored packages as suggestions the user can browse.
    /// </summary>
    public IReadOnlyList<SuggestedPackage>? SuggestedPackages { get; init; }

    public bool IsSuccessful => ChosenPackageId.HasValue && Confidence >= 0.6;
}

/// <summary>
/// A package suggestion shown to the user when no confident match was found.
/// </summary>
public sealed record SuggestedPackage(Guid Id, string Title, string Description);
