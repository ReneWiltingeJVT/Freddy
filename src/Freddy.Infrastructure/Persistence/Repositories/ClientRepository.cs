#pragma warning disable IDE0058 // Expression value is never used — repository operations

using Freddy.Application.Common.Interfaces;
using Freddy.Application.Entities;
using Microsoft.EntityFrameworkCore;

namespace Freddy.Infrastructure.Persistence.Repositories;

public sealed class ClientRepository(FreddyDbContext dbContext) : IClientRepository
{
    public async Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Client>> GetAllAsync(bool? isActive, string? search, CancellationToken cancellationToken)
    {
        IQueryable<Client> query = dbContext.Clients.AsNoTracking();

        if (isActive.HasValue)
        {
            query = query.Where(c => c.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c =>
                EF.Functions.ILike(c.DisplayName, $"%{search}%") ||
                c.Aliases.Any(a => EF.Functions.ILike(a, $"%{search}%")));
        }

        return await query
            .OrderBy(c => c.DisplayName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Client?> FindByAliasAsync(string alias, CancellationToken cancellationToken)
    {
        string normalizedAlias = alias.Trim().ToLowerInvariant();

        return await dbContext.Clients
            .AsNoTracking()
            .Where(c => c.IsActive && (
                EF.Functions.ILike(c.DisplayName, normalizedAlias) ||
                c.Aliases.Any(a => EF.Functions.ILike(a, normalizedAlias))))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Client> CreateAsync(Client client, CancellationToken cancellationToken)
    {
        dbContext.Clients.Add(client);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return client;
    }

    public async Task<Client> UpdateAsync(Client client, CancellationToken cancellationToken)
    {
        dbContext.Clients.Update(client);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return client;
    }

    public async Task DeleteAsync(Client client, CancellationToken cancellationToken)
    {
        dbContext.Clients.Remove(client);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
