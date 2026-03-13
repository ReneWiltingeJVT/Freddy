namespace Freddy.Application.Entities;

/// <summary>
/// Append-only audit trail for sensitive operations such as personal plan access.
/// </summary>
public sealed class AuditLog
{
    public Guid Id { get; set; }

    /// <summary>
    /// The user who performed the action (maps to conversation UserId or admin identity).
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The action performed (e.g., "PersonalPlanAccessed", "ClientCreated", "PackageUpdated").
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// The type of entity involved (e.g., "Package", "Client").
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the entity involved.
    /// </summary>
    public Guid EntityId { get; set; }

    /// <summary>
    /// Optional JSON details about the action (e.g., which client was accessed).
    /// </summary>
    public string? Details { get; set; }

    public DateTimeOffset Timestamp { get; set; }
}
