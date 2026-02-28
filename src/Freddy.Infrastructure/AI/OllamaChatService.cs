using Freddy.Application.Common;
using Freddy.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Freddy.Infrastructure.AI;

public sealed class OllamaChatService(
    IChatCompletionService chatCompletion,
    ILogger<OllamaChatService> logger) : IChatService
{
    private const string SystemPrompt = """
        Je bent Freddy, een behulpzame digitale assistent voor zorgmedewerkers in de thuiszorg.
        Je helpt met vragen over protocollen, procedures en werkwijzen.
        Antwoord altijd in het Nederlands, op B1 taalniveau.
        Houd je antwoorden kort en duidelijk (maximaal een paar alinea's).
        Als je het antwoord niet weet, zeg dat eerlijk en verwijs naar de leidinggevende.
        """;

    public async Task<Result<string>> GetResponseAsync(string userMessage, CancellationToken cancellationToken)
    {
        try
        {
            var chatHistory = new ChatHistory(SystemPrompt);
            chatHistory.AddUserMessage(userMessage);

            logger.LogInformation("Sending message to LLM ({MessageLength} chars)", userMessage.Length);

            ChatMessageContent response = await chatCompletion.GetChatMessageContentAsync(
                chatHistory,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            string? content = response.Content;
            if (string.IsNullOrWhiteSpace(content))
            {
                logger.LogWarning("LLM returned empty response");
                return Result<string>.Failure("Geen antwoord ontvangen van de AI service.");
            }

            logger.LogInformation("LLM response received ({ResponseLength} chars)", content.Length);
            return Result<string>.Success(content);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Error communicating with LLM");
            return Result<string>.Failure("Er is een fout opgetreden bij het verwerken van je vraag.");
        }
    }
}
