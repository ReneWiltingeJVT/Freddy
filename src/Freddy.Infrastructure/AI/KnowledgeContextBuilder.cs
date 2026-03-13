using System.Text;
using Freddy.Application.Common.Interfaces;
using Freddy.Application.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Freddy.Infrastructure.AI;

/// <summary>
/// Builds a compact knowledge context for injection into the LLM system prompt.
/// Caches the package/client overviews for 5 minutes to avoid repeated DB queries.
/// </summary>
public sealed class KnowledgeContextBuilder(
    IPackageRepository packageRepository,
    IClientRepository clientRepository,
    IMemoryCache cache,
    ILogger<KnowledgeContextBuilder> logger) : IKnowledgeContextBuilder
{
    private const string PackageCacheKey = "knowledge:packages";
    private const string ClientCacheKey = "knowledge:clients";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<KnowledgeContext> BuildAsync(Guid? clientId, CancellationToken cancellationToken)
    {
        string packageSummaries = await GetOrBuildPackageSummariesAsync(cancellationToken).ConfigureAwait(false);
        string clientInfo = await GetOrBuildClientInfoAsync(cancellationToken).ConfigureAwait(false);
        string personalPlans = clientId.HasValue
            ? await BuildPersonalPlansAsync(clientId.Value, cancellationToken).ConfigureAwait(false)
            : string.Empty;

        var context = new KnowledgeContext(packageSummaries, clientInfo, personalPlans);

        logger.LogInformation(
            "[KnowledgeContext] Built context: {TotalLength} chars (packages={PkgLen}, clients={ClientLen}, plans={PlanLen})",
            context.TotalLength, packageSummaries.Length, clientInfo.Length, personalPlans.Length);

        return context;
    }

    private async Task<string> GetOrBuildPackageSummariesAsync(CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(PackageCacheKey, out string? cached) && cached is not null)
        {
            return cached;
        }

        IReadOnlyList<Package> packages = await packageRepository
            .GetAllPublishedAsync(cancellationToken).ConfigureAwait(false);

        if (packages.Count == 0)
        {
            return "Er zijn momenteel geen pakketten beschikbaar.";
        }

        var sb = new StringBuilder();

        // Group by category for clarity
        IOrderedEnumerable<IGrouping<PackageCategory, Package>> grouped = packages
            .Where(p => p.Category != PackageCategory.PersonalPlan)
            .GroupBy(p => p.Category)
            .OrderBy(g => (int)g.Key);

        foreach (IGrouping<PackageCategory, Package> group in grouped)
        {
            string categoryName = CategoryDisplayName(group.Key);
            _ = sb.Append('\n').Append(categoryName).AppendLine("s:");

            foreach (Package p in group.OrderBy(p => p.Title, StringComparer.Ordinal))
            {
                _ = sb.Append("- ").Append(p.Title).Append(": ").AppendLine(p.Description);

                if (p.Tags.Count > 0)
                {
                    _ = sb.Append("  Onderwerpen: ").AppendLine(string.Join(", ", p.Tags.Take(8)));
                }
            }
        }

        string result = sb.ToString().TrimEnd();
        cache.Set(PackageCacheKey, result, CacheDuration);
        return result;
    }

    private async Task<string> GetOrBuildClientInfoAsync(CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(ClientCacheKey, out string? cached) && cached is not null)
        {
            return cached;
        }

        IReadOnlyList<Client> clients = await clientRepository
            .GetAllAsync(isActive: true, search: null, cancellationToken).ConfigureAwait(false);

        if (clients.Count == 0)
        {
            return "Er zijn geen cliënten bekend.";
        }

        var sb = new StringBuilder();
        _ = sb.AppendLine("Bekende cliënten:");

        foreach (Client client in clients.OrderBy(c => c.DisplayName, StringComparer.Ordinal))
        {
            _ = sb.Append("- ").AppendLine(client.DisplayName);
        }

        string result = sb.ToString().TrimEnd();
        cache.Set(ClientCacheKey, result, CacheDuration);
        return result;
    }

    private async Task<string> BuildPersonalPlansAsync(Guid clientId, CancellationToken cancellationToken)
    {
        IReadOnlyList<Package> plans = await packageRepository
            .GetPublishedByClientIdAsync(clientId, cancellationToken).ConfigureAwait(false);

        if (plans.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        _ = sb.AppendLine("Persoonlijke plannen voor deze cliënt:");

        foreach (Package plan in plans.OrderBy(p => p.Title, StringComparer.Ordinal))
        {
            _ = sb.Append("- ").Append(plan.Title).Append(": ").AppendLine(plan.Description);
        }

        return sb.ToString().TrimEnd();
    }

    private static string CategoryDisplayName(PackageCategory category) => category switch
    {
        PackageCategory.Protocol => "Protocol",
        PackageCategory.WorkInstruction => "Werkinstructie",
        PackageCategory.PersonalPlan => "Persoonlijk plan",
        _ => "Pakket",
    };
}
