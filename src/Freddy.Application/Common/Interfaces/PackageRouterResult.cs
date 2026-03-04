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

    public bool IsSuccessful => ChosenPackageId.HasValue && Confidence >= 0.6;
}
