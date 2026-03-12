namespace Freddy.Application.Common.Interfaces;

/// <summary>
/// Detects a client name in a user message using deterministic (alias) matching.
/// Returns the matched client ID, or null if no client was detected.
/// </summary>
public interface IClientDetector
{
    /// <summary>
    /// Attempts to find a client by checking if any registered client alias or display name
    /// appears in the user message.
    /// </summary>
    Task<ClientDetectionResult> DetectAsync(string userMessage, CancellationToken cancellationToken);
}

/// <summary>
/// Result of client detection. Contains the client ID if a match was found.
/// </summary>
public sealed record ClientDetectionResult
{
    public Guid? ClientId { get; init; }
    public string? MatchedName { get; init; }
    public bool IsDetected => ClientId.HasValue;

    public static ClientDetectionResult NoMatch { get; } = new();
}
