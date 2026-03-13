namespace Freddy.Infrastructure.AI;

/// <summary>
/// Configuration options for AI services (LLM provider, models, parameters).
/// Bound from the "AI" section in appsettings.json.
/// </summary>
public sealed class AIOptions
{
    public const string SectionName = "AI";

    /// <summary>AI provider: "Ollama" (default) or "OpenAI" (for OpenAI-compatible APIs like OpenRouter).</summary>
    public string Provider { get; set; } = "Ollama";

    /// <summary>Base endpoint URL for the AI service.</summary>
    public string Endpoint { get; set; } = "http://localhost:11434";

    /// <summary>Model used for conversational chat responses (larger, better reasoning).</summary>
    public string ChatModelId { get; set; } = "llama3.1:8b";

    /// <summary>Model used for lightweight classification tasks (smaller, faster).</summary>
    public string ClassifierModelId { get; set; } = "qwen2.5:1.5b";

    /// <summary>HTTP timeout in seconds for AI requests.</summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>Maximum tokens for chat responses.</summary>
    public int MaxTokens { get; set; } = 1024;

    /// <summary>Temperature for chat responses (0.0 = deterministic, 1.0 = creative).</summary>
    public double Temperature { get; set; } = 0.1;
}
