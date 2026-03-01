using System.Text.Json;
using Freddy.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Freddy.Infrastructure.AI;

public sealed class OllamaPackageRouter(
    IChatCompletionService chatCompletion,
    ILogger<OllamaPackageRouter> logger) : IPackageRouter
{
    private const string SystemPrompt = """
        Je bent een classificatie-engine. Je ontvangt een gebruikersvraag en een lijst met beschikbare pakketten.
        Je taak is UITSLUITEND om te bepalen welk pakket het beste past bij de vraag.

        Je MOET antwoorden in exact dit JSON-formaat, ZONDER extra tekst:
        {
          "chosenPackageId": "<guid of het best passende pakket, of null als geen pakket past>",
          "confidence": <getal tussen 0.0 en 1.0>,
          "needsConfirmation": <true als je niet zeker bent en bevestiging nodig is>,
          "reason": "<korte reden voor je keuze in het Nederlands>"
        }

        Regels:
        - Als geen enkel pakket past, zet chosenPackageId op null en confidence op 0.0
        - Bij twijfel tussen pakketten, kies het meest waarschijnlijke en zet needsConfirmation op true
        - confidence >= 0.8: direct antwoord geven
        - confidence 0.6-0.8: bevestiging vragen
        - confidence < 0.6: geen match
        - Antwoord ALLEEN met geldig JSON, geen andere tekst
        """;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task<PackageRouterResult> RouteAsync(
        string userMessage,
        IReadOnlyList<PackageCandidate> candidates,
        CancellationToken cancellationToken)
    {
        if (candidates.Count == 0)
        {
            logger.LogInformation("No package candidates available for routing");
            return new PackageRouterResult
            {
                ChosenPackageId = null,
                Confidence = 0.0,
                NeedsConfirmation = false,
                Reason = "Geen pakketten beschikbaar.",
            };
        }

        string candidateList = FormatCandidates(candidates);
        string userPrompt = $"""
            Gebruikersvraag: "{userMessage}"

            Beschikbare pakketten:
            {candidateList}

            Welk pakket past het beste? Antwoord ALLEEN met JSON.
            """;

        try
        {
            var chatHistory = new ChatHistory(SystemPrompt);
            chatHistory.AddUserMessage(userPrompt);

            logger.LogInformation("Routing message to package classifier ({CandidateCount} candidates)", candidates.Count);

            ChatMessageContent response = await chatCompletion.GetChatMessageContentAsync(
                chatHistory,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            string? rawContent = response.Content;
            if (string.IsNullOrWhiteSpace(rawContent))
            {
                logger.LogWarning("Package router returned empty response");
                return CreateFallbackResult();
            }

            logger.LogDebug("Package router raw response: {RawResponse}", rawContent);
            return ParseRouterResponse(rawContent, candidates);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "AI service unreachable during package routing");
            return CreateServiceUnavailableResult();
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "AI service timed out during package routing");
            return CreateServiceUnavailableResult();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Error during package routing");
            return CreateFallbackResult();
        }
    }

    private static string FormatCandidates(IReadOnlyList<PackageCandidate> candidates)
    {
        return string.Join("\n", candidates.Select(c =>
            $"- ID: {c.Id} | Naam: {c.Title} | Beschrijving: {c.Description}"));
    }

    private PackageRouterResult ParseRouterResponse(string rawJson, IReadOnlyList<PackageCandidate> candidates)
    {
        // Strip potential markdown code fences that LLMs sometimes add
        string cleaned = rawJson.Trim();
        if (cleaned.StartsWith("```", StringComparison.Ordinal))
        {
            int firstNewline = cleaned.IndexOf('\n', StringComparison.Ordinal);
            int lastFence = cleaned.LastIndexOf("```", StringComparison.Ordinal);
            if (firstNewline >= 0 && lastFence > firstNewline)
            {
                cleaned = cleaned[(firstNewline + 1)..lastFence].Trim();
            }
        }

        try
        {
            RouterJsonResponse? parsed = JsonSerializer.Deserialize<RouterJsonResponse>(cleaned, JsonOptions);
            if (parsed is null)
            {
                logger.LogWarning("Failed to deserialize router JSON response");
                return CreateFallbackResult();
            }

            // Validate: chosenPackageId must exist in our candidate list
            if (parsed.ChosenPackageId.HasValue &&
                candidates.All(c => c.Id != parsed.ChosenPackageId.Value))
            {
                logger.LogWarning("Router returned packageId {PackageId} which is not in candidate list — rejecting",
                    parsed.ChosenPackageId.Value);
                return CreateFallbackResult();
            }

            // Clamp confidence to valid range
            double confidence = Math.Clamp(parsed.Confidence, 0.0, 1.0);

            return new PackageRouterResult
            {
                ChosenPackageId = parsed.ChosenPackageId,
                Confidence = confidence,
                NeedsConfirmation = parsed.NeedsConfirmation || (confidence >= 0.6 && confidence < 0.8),
                Reason = parsed.Reason,
            };
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse router JSON: {RawJson}", cleaned);
            return CreateFallbackResult();
        }
    }

    private static PackageRouterResult CreateFallbackResult() => new()
    {
        ChosenPackageId = null,
        Confidence = 0.0,
        NeedsConfirmation = false,
        Reason = "Kon de vraag niet classificeren.",
    };

    private static PackageRouterResult CreateServiceUnavailableResult() => new()
    {
        ChosenPackageId = null,
        Confidence = 0.0,
        NeedsConfirmation = false,
        Reason = "AI-service is niet bereikbaar.",
        IsServiceUnavailable = true,
    };

    private sealed record RouterJsonResponse
    {
        public Guid? ChosenPackageId { get; init; }

        public double Confidence { get; init; }

        public bool NeedsConfirmation { get; init; }

        public string? Reason { get; init; }
    }
}
