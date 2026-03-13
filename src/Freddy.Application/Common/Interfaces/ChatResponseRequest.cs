using Freddy.Application.Entities;

namespace Freddy.Application.Common.Interfaces;

/// <summary>
/// Input for the conversational response generator.
/// </summary>
/// <param name="UserMessage">The user's current question.</param>
/// <param name="KnowledgeContext">Overview of all available packages and clients.</param>
/// <param name="MatchedPackageTitle">Title of the package matched by routing (null if no match).</param>
/// <param name="MatchedPackageContent">Full content of the matched package (null if no match).</param>
/// <param name="ConversationHistory">Recent messages for multi-turn context.</param>
public sealed record ChatResponseRequest(
    string UserMessage,
    KnowledgeContext KnowledgeContext,
    string? MatchedPackageTitle,
    string? MatchedPackageContent,
    IReadOnlyList<Message> ConversationHistory);
