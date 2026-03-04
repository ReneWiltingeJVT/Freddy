using Freddy.Application.Entities;

namespace Freddy.Application.Common.Interfaces;

public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Document>> GetByPackageIdAsync(Guid packageId, CancellationToken cancellationToken);

    Task<Document> CreateAsync(Document document, CancellationToken cancellationToken);

    Task<Document> UpdateAsync(Document document, CancellationToken cancellationToken);

    Task DeleteAsync(Document document, CancellationToken cancellationToken);
}
