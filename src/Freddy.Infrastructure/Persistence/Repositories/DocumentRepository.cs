#pragma warning disable IDE0058 // Expression value is never used — repository operations

using Freddy.Application.Common.Interfaces;
using Freddy.Application.Entities;
using Microsoft.EntityFrameworkCore;

namespace Freddy.Infrastructure.Persistence.Repositories;

public sealed class DocumentRepository(FreddyDbContext dbContext) : IDocumentRepository
{
    public async Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Document>> GetByPackageIdAsync(Guid packageId, CancellationToken cancellationToken)
    {
        return await dbContext.Documents
            .AsNoTracking()
            .Where(d => d.PackageId == packageId)
            .OrderBy(d => d.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Document> CreateAsync(Document document, CancellationToken cancellationToken)
    {
        dbContext.Documents.Add(document);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return document;
    }

    public async Task<Document> UpdateAsync(Document document, CancellationToken cancellationToken)
    {
        dbContext.Documents.Update(document);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return document;
    }

    public async Task DeleteAsync(Document document, CancellationToken cancellationToken)
    {
        dbContext.Documents.Remove(document);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
