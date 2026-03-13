#pragma warning disable IDE0058 // Expression value is never used — repository operations

using Freddy.Application.Common.Interfaces;
using Freddy.Application.Entities;
using Microsoft.EntityFrameworkCore;

namespace Freddy.Infrastructure.Persistence.Repositories;

public sealed class AuditLogRepository(FreddyDbContext dbContext) : IAuditLogRepository
{
    public async Task LogAsync(
        Guid userId,
        string action,
        string entityType,
        Guid entityId,
        string? details,
        CancellationToken cancellationToken)
    {
        var log = new AuditLog
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            Timestamp = DateTimeOffset.UtcNow,
        };

        dbContext.AuditLogs.Add(log);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AuditLog>> GetByEntityAsync(
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken)
    {
        return await dbContext.AuditLogs
            .AsNoTracking()
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
