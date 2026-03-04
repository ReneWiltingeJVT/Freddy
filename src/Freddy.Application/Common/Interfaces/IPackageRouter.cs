namespace Freddy.Application.Common.Interfaces;

/// <summary>
/// Routes a user question to a specific package using the LLM as a JSON-only classifier.
/// </summary>
public interface IPackageRouter
{
    Task<PackageRouterResult> RouteAsync(string userMessage, IReadOnlyList<PackageCandidate> candidates, CancellationToken cancellationToken);
}
