using Freddy.Application.Common.Interfaces;
using Freddy.Application.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Freddy.Infrastructure.AI;

/// <summary>
/// Generates conversational responses by combining a knowledge context, matched package content,
/// and conversation history into a structured LLM prompt. Uses the "chat" model (larger, better reasoning)
/// rather than the lightweight classifier model.
/// </summary>
public sealed class ChatResponseGenerator(
    [FromKeyedServices("chat")] IChatCompletionService chatCompletion,
    IOptions<AIOptions> aiOptions,
    ILogger<ChatResponseGenerator> logger) : IChatResponseGenerator
{
    private const string SystemPromptTemplate = """
        Je bent Freddy, een vriendelijke en behulpzame digitale assistent voor zorgmedewerkers in de thuiszorg.
        Je helpt medewerkers snel de juiste informatie te vinden over protocollen, werkinstructies en persoonlijke plannen van cliënten.

        ## REGELS — deze zijn strikt en je moet ze altijd volgen:
        1. Beantwoord vragen UITSLUITEND op basis van de onderstaande informatie. Verzin NIETS.
        2. Als de informatie het antwoord niet bevat: zeg dat eerlijk en verwijs naar de leidinggevende.
        3. Verwijs bij inhoudelijke antwoorden altijd naar het bronpakket bij naam (bijv. "Volgens Protocol Agressie...").
        4. Gebruik eenvoudige, duidelijke taal op B1 niveau.
        5. Geef NOOIT medisch advies over individuele patiënten. Verwijs naar de arts of leidinggevende.
        6. Bij twijfel: verwijs door naar de leidinggevende.
        7. Houd antwoorden bondig maar volledig. Gebruik opsommingen waar dat helpt.
        8. Als een gebruiker vraagt welke pakketten, protocollen, werkinstructies of cliënten er zijn,
           gebruik de onderstaande overzichtsinformatie om een compleet antwoord te geven.
        9. Als een gebruiker een cliëntnaam noemt, gebruik dan de persoonlijke plannen van die cliënt als context.

        ## BESCHIKBARE KENNIS
        {PACKAGE_SUMMARIES}

        ## CLIËNTINFORMATIE
        {CLIENT_INFO}

        {PERSONAL_PLANS_SECTION}

        {MATCHED_PACKAGE_SECTION}
        """;

    private const string MatchedPackageSectionTemplate = """
        ## RELEVANT PAKKET — gebruik dit als primaire bron voor je antwoord
        Pakket: {TITLE}

        {CONTENT}
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
            : $"## PERSOONLIJKE PLANNEN CLIËNT\n{request.KnowledgeContext.PersonalPlans}";

        string matchedPackageSection = request.MatchedPackageTitle is not null && request.MatchedPackageContent is not null
            ? MatchedPackageSectionTemplate
                .Replace("{TITLE}", request.MatchedPackageTitle, StringComparison.Ordinal)
                .Replace("{CONTENT}", request.MatchedPackageContent, StringComparison.Ordinal)
            : string.Empty;

        return SystemPromptTemplate
            .Replace("{PACKAGE_SUMMARIES}", request.KnowledgeContext.PackageSummaries, StringComparison.Ordinal)
            .Replace("{CLIENT_INFO}", request.KnowledgeContext.ClientInfo, StringComparison.Ordinal)
            .Replace("{PERSONAL_PLANS_SECTION}", personalPlansSection, StringComparison.Ordinal)
            .Replace("{MATCHED_PACKAGE_SECTION}", matchedPackageSection, StringComparison.Ordinal);
    }
}
