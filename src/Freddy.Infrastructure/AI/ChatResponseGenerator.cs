using Freddy.Application.Common.Interfaces;
using Freddy.Application.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Freddy.Infrastructure.AI;

/// <summary>
/// Generates conversational responses for overview and unmatched queries.
/// Uses the classifier model (qwen2.5:1.5b) with a slim prompt — matched package
/// responses are handled by <see cref="PackageResponseFormatter"/> without any LLM call.
/// </summary>
public sealed class ChatResponseGenerator(
    [FromKeyedServices("classifier")] IChatCompletionService chatCompletion,
    IOptions<AIOptions> aiOptions,
    ILogger<ChatResponseGenerator> logger) : IChatResponseGenerator
{
    // Slim system prompt used only for overview / unmatched queries.
    // Matched packages take the deterministic PackageResponseFormatter path instead.
    private const string SystemPromptTemplate = """
        Je bent Freddy, een digitale assistent voor zorgmedewerkers in de thuiszorg.
        Je beantwoordt overzichtsvragen over beschikbare protocollen, werkinstructies en cliënten.

        ## REGELS
        1. Beantwoord uitsluitend op basis van onderstaande kennis. Verzin niets.
        2. Bevat de kennis het antwoord niet: zeg dat eerlijk en verwijs naar de leidinggevende.
        3. Gebruik eenvoudige taal op B1 niveau.
        4. Geef korte, overzichtelijke antwoorden. Gebruik opsommingen waar dat helpt.
        5. Geef NOOIT medisch advies. Verwijs naar de arts of leidinggevende.

        ## BESCHIKBARE PAKKETTEN
        {PACKAGE_SUMMARIES}

        ## CLIËNTINFORMATIE
        {CLIENT_INFO}

        {PERSONAL_PLANS_SECTION}
        """;

    public async Task<ChatResponseResult> GenerateAsync(
        ChatResponseRequest request,
        CancellationToken cancellationToken)
    {
        AIOptions options = aiOptions.Value;

        string systemPrompt = BuildSystemPrompt(request);

        var chatHistory = new ChatHistory(systemPrompt);

        // Add conversation history for multi-turn context (limited to last messages)
        foreach (Message msg in request.ConversationHistory)
        {
            if (msg.Role == MessageRole.User)
            {
                chatHistory.AddUserMessage(msg.Content);
            }
            else if (msg.Role == MessageRole.Assistant)
            {
                chatHistory.AddAssistantMessage(msg.Content);
            }
        }

        // Add current user message
        chatHistory.AddUserMessage(request.UserMessage);

        logger.LogInformation(
            "[ChatResponse] Generating response. HistoryMessages={HistoryCount}, HasMatchedPackage={HasPackage}, SystemPromptLength={PromptLen}",
            request.ConversationHistory.Count,
            request.MatchedPackageTitle is not null,
            systemPrompt.Length);

        try
        {
#pragma warning disable SKEXP0070 // Ollama connector is experimental
            var executionSettings = new Microsoft.SemanticKernel.Connectors.Ollama.OllamaPromptExecutionSettings
            {
                Temperature = (float)options.Temperature,
                NumPredict = options.MaxTokens,
            };
#pragma warning restore SKEXP0070

            ChatMessageContent response = await chatCompletion.GetChatMessageContentAsync(
                chatHistory,
                executionSettings,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            string? content = response.Content;
            if (string.IsNullOrWhiteSpace(content))
            {
                logger.LogWarning("[ChatResponse] LLM returned empty response");
                return ChatResponseResult.Fallback(
                    "Sorry, ik kon geen antwoord genereren. Probeer je vraag anders te formuleren.");
            }

            logger.LogInformation(
                "[ChatResponse] Response generated: {ResponseLength} chars",
                content.Length);

            return new ChatResponseResult(
                content.Trim(),
                request.MatchedPackageTitle,
                IsGrounded: request.MatchedPackageTitle is not null || request.KnowledgeContext.TotalLength > 0);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "[ChatResponse] AI service unreachable");
            return ChatResponseResult.Fallback(
                "De AI-service is momenteel niet beschikbaar. Probeer het later opnieuw of neem contact op met je leidinggevende.");
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "[ChatResponse] AI service timed out");
            return ChatResponseResult.Fallback(
                "Het genereren van een antwoord duurde te lang. Probeer het opnieuw of stel een kortere vraag.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "[ChatResponse] Unexpected error generating response");
            return ChatResponseResult.Fallback(
                "Er is een onverwachte fout opgetreden. Probeer het opnieuw.");
        }
    }

    private static string BuildSystemPrompt(ChatResponseRequest request)
    {
        string personalPlansSection = string.IsNullOrWhiteSpace(request.KnowledgeContext.PersonalPlans)
            ? string.Empty
            : $"## PERSOONLIJKE PLANNEN\n{request.KnowledgeContext.PersonalPlans}";

        return SystemPromptTemplate
            .Replace("{PACKAGE_SUMMARIES}", request.KnowledgeContext.PackageSummaries, StringComparison.Ordinal)
            .Replace("{CLIENT_INFO}", request.KnowledgeContext.ClientInfo, StringComparison.Ordinal)
            .Replace("{PERSONAL_PLANS_SECTION}", personalPlansSection, StringComparison.Ordinal);
    }
}
