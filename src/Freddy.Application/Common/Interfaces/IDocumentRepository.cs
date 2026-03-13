using Freddy.Application.Entities;

namespace Freddy.Application.Common.Interfaces;

public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Document>> GetByPackageIdAsync(Guid packageId, CancellationToken cancellationToken);

    /// <summary>
    /// Returns a mapping of package ID → document names for all provided package IDs.
    /// Executes a single DB query instead of one per package (avoids N+1).
    /// </summary>
    Task<Dictionary<Guid, List<string>>> GetNamesByPackageIdsAsync(
        IEnumerable<Guid> packageIds,
        CancellationToken cancellationToken);

    Task<Document> CreateAsync(Document document, CancellationToken cancellationToken);

    Task<Document> UpdateAsync(Document document, CancellationToken cancellationToken);

    Task DeleteAsync(Document document, CancellationToken cancellationToken);
}
