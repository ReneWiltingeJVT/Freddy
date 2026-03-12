namespace Freddy.Application.Entities;

/// <summary>
/// A care client whose personal plans can be retrieved via scoped queries.
/// </summary>
public sealed class Client
{
    public Guid Id { get; set; }

    /// <summary>
    /// The display name shown in chat responses (e.g., "Meneer van het Hout").
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Alternative names/aliases used for deterministic name matching (e.g., "Van het Hout", "Dhr. van het Hout").
    /// Stored as PostgreSQL text[] with GIN index for fast lookups.
    /// </summary>
    public ICollection<string> Aliases { get; set; } = [];

    /// <summary>
    /// Soft-delete flag — inactive clients are excluded from name matching.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Navigation property to personal plan packages linked to this client.
    /// </summary>
    public ICollection<Package> Packages { get; set; } = [];
}
