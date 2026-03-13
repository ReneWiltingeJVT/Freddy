namespace Freddy.Application.Common.Interfaces;

/// <summary>
/// Generates conversational chat responses using an LLM.
/// Instead of dumping raw package content, the LLM reads the relevant context
/// and formulates a helpful, natural-language answer grounded in the provided information.
/// </summary>
public interface IChatResponseGenerator
{
    /// <summary>
    /// Generates a conversational response based on the user's question,
    /// knowledge context, and optionally a matched package's full content.
    /// </summary>
    Task<ChatResponseResult> GenerateAsync(
        ChatResponseRequest request,
        CancellationToken cancellationToken);
}
