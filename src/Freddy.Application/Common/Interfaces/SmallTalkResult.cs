namespace Freddy.Application.Common.Interfaces;

/// <summary>
/// Result of small talk detection. Contains the detected category and template response.
/// When <see cref="Category"/> is <see cref="SmallTalkCategory.None"/>,
/// <see cref="TemplateResponse"/> is <c>null</c> and the message should be routed normally.
/// </summary>
public sealed record SmallTalkResult(SmallTalkCategory Category, string? TemplateResponse = null)
{
    /// <summary>
    /// Indicates that the message is not small talk and should proceed to routing.
    /// </summary>
    public static SmallTalkResult NoMatch { get; } = new(SmallTalkCategory.None, null);

    /// <summary>
    /// Whether a small talk category was detected.
    /// </summary>
    public bool IsSmallTalk => Category != SmallTalkCategory.None;
}
