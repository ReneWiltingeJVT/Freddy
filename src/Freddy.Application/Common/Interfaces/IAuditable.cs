using Freddy.Application.Common.Interfaces;

namespace Freddy.Application.Common.Interfaces;

/// <summary>
/// Marker interface for commands that should be audit-logged.
/// Implementations record the action in the audit log after successful execution.
/// </summary>
public interface IAuditable
{
    string AuditAction { get; }
    string AuditEntityType { get; }
    Guid AuditEntityId { get; }
}
