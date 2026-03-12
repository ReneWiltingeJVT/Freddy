using Freddy.Application.Entities;

namespace Freddy.Application.Common.Interfaces;

public interface IAuditLogRepository
{
    Task LogAsync(Guid userId, string action, string entityType, Guid entityId, string? details, CancellationToken cancellationToken);

    Task<IReadOnlyList<AuditLog>> GetByEntityAsync(string entityType, Guid entityId, CancellationToken cancellationToken);
}
