namespace Freddy.Application.Common.Interfaces;

/// <summary>
/// Builds a knowledge context summary that is injected into the LLM system prompt.
/// Contains an overview of all available packages and client information so the LLM
/// knows what exists without needing the full content of every package.
/// </summary>
public interface IKnowledgeContextBuilder
{
    /// <summary>
    /// Builds the knowledge context. When a <paramref name="clientId"/> is provided,
    /// includes that client's personal plans in the context.
    /// </summary>
    Task<KnowledgeContext> BuildAsync(Guid? clientId, CancellationToken cancellationToken);
}
