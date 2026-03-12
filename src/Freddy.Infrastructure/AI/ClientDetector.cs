using Freddy.Application.Common.Interfaces;
using Freddy.Application.Entities;
using Microsoft.Extensions.Logging;

namespace Freddy.Infrastructure.AI;

/// <summary>
/// Deterministic client detector that finds client names/aliases in user messages.
/// Uses exact case-insensitive substring matching against all active clients' display names and aliases.
/// Performance: O(clients × aliases) per message — suitable for small-to-medium client sets.
/// </summary>
public sealed class ClientDetector(
    IClientRepository clientRepository,
    ILogger<ClientDetector> logger) : IClientDetector
{
    public async Task<ClientDetectionResult> DetectAsync(string userMessage, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            return ClientDetectionResult.NoMatch;
        }

        string normalized = userMessage.Trim().ToLowerInvariant();

        IReadOnlyList<Client> clients = await clientRepository
            .GetAllAsync(isActive: true, search: null, cancellationToken)
            .ConfigureAwait(false);

        // Try matching display names first (longer names match first to avoid partial collisions)
        foreach (Client client in clients.OrderByDescending(c => c.DisplayName.Length))
        {
            if (normalized.Contains(client.DisplayName.ToLowerInvariant(), StringComparison.Ordinal))
            {
                logger.LogInformation(
                    "[ClientDetector] Matched client {ClientId} by display name '{DisplayName}'",
                    client.Id, client.DisplayName);

                return new ClientDetectionResult { ClientId = client.Id, MatchedName = client.DisplayName };
            }
        }

        // Try aliases (ordered by length descending for longest-match-first)
        foreach (Client client in clients)
        {
            foreach (string alias in client.Aliases.OrderByDescending(a => a.Length))
            {
                if (alias.Length >= 3 && normalized.Contains(alias.ToLowerInvariant(), StringComparison.Ordinal))
                {
                    logger.LogInformation(
                        "[ClientDetector] Matched client {ClientId} by alias '{Alias}'",
                        client.Id, alias);

                    return new ClientDetectionResult { ClientId = client.Id, MatchedName = alias };
                }
            }
        }

        logger.LogDebug("[ClientDetector] No client detected in message");
        return ClientDetectionResult.NoMatch;
    }
}
