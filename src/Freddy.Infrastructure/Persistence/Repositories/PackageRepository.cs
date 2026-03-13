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
            .Where(p => p.IsPublished &&
                (EF.Functions.ILike(p.Title, $"%{normalizedQuery}%") ||
                 EF.Functions.ILike(p.Description, $"%{normalizedQuery}%") ||
                 p.Tags.Any(k => EF.Functions.ILike(k, $"%{normalizedQuery}%")) ||
                 p.Synonyms.Any(s => EF.Functions.ILike(s, $"%{normalizedQuery}%"))))
            .OrderBy(p => p.Title)
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

    public async Task<IReadOnlyList<Package>> GetAllPublishedAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Packages
            .AsNoTracking()
            .Where(p => p.IsPublished)
            .OrderBy(p => p.Title)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Package>> GetPublishedByClientIdAsync(Guid clientId, CancellationToken cancellationToken)
    {
        return await dbContext.Packages
            .AsNoTracking()
            .Where(p => p.IsPublished && p.ClientId == clientId)
            .OrderBy(p => p.Title)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Package>> GetAllAsync(bool? isPublished, string? search, PackageCategory? category, CancellationToken cancellationToken)
    {
        IQueryable<Package> query = dbContext.Packages.AsNoTracking();

        if (isPublished.HasValue)
        {
            query = query.Where(p => p.IsPublished == isPublished.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p =>
                EF.Functions.ILike(p.Title, $"%{search}%") ||
                EF.Functions.ILike(p.Description, $"%{search}%"));
        }

        if (category.HasValue)
        {
            query = query.Where(p => p.Category == category.Value);
        }

        return await query
            .OrderBy(p => p.Title)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Package> CreateAsync(Package package, CancellationToken cancellationToken)
    {
        dbContext.Packages.Add(package);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return package;
    }

    public async Task<Package> UpdateAsync(Package package, CancellationToken cancellationToken)
    {
        dbContext.Packages.Update(package);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return package;
    }

    public async Task DeleteAsync(Package package, CancellationToken cancellationToken)
    {
        dbContext.Packages.Remove(package);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
