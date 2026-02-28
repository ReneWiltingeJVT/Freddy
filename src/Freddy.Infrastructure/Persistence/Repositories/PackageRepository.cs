#pragma warning disable IDE0058 // Expression value is never used — repository operations

using Freddy.Application.Common.Interfaces;
using Freddy.Application.Entities;
using Microsoft.EntityFrameworkCore;

namespace Freddy.Infrastructure.Persistence.Repositories;

public sealed class PackageRepository(FreddyDbContext dbContext) : IPackageRepository
{
    public async Task<IReadOnlyList<Package>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        string normalizedQuery = query.ToUpperInvariant();

        return await dbContext.Packages
            .AsNoTracking()
            .Where(p => p.IsActive &&
                (EF.Functions.ILike(p.Name, $"%{normalizedQuery}%") ||
                 EF.Functions.ILike(p.Description, $"%{normalizedQuery}%") ||
                 p.Keywords.Any(k => EF.Functions.ILike(k, $"%{normalizedQuery}%"))))
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Package?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Packages
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Package>> GetAllActiveAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Packages
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
