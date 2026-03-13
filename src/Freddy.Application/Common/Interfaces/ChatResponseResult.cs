namespace Freddy.Application.Common.Interfaces;

/// <summary>
/// Result from the conversational response generator.
/// </summary>
/// <param name="Content">The generated response text.</param>
/// <param name="SourcePackageTitle">The package title referenced in the response (null if general answer).</param>
/// <param name="IsGrounded">Whether the LLM confirmed its answer is based on provided context.</param>
public sealed record ChatResponseResult(
    string Content,
    string? SourcePackageTitle,
    bool IsGrounded)
{
    /// <summary>Creates a fallback result when the LLM is unavailable.</summary>
    public static ChatResponseResult Fallback(string content) => new(content, null, false);
}
