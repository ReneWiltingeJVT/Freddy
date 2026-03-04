namespace Freddy.Application.Common.Interfaces;

/// <summary>
/// Detects small talk messages (greetings, thanks, farewell, help intent, confusion)
/// and returns a template response without invoking the AI pipeline.
/// </summary>
public interface ISmallTalkDetector
{
    /// <summary>
    /// Checks whether the given message is small talk.
    /// Returns a <see cref="SmallTalkResult"/> with the detected category and template response,
    /// or <see cref="SmallTalkCategory.None"/> if the message should be routed normally.
    /// </summary>
    SmallTalkResult Detect(string message);
}
