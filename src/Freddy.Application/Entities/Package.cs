namespace Freddy.Application.Entities;

/// <summary>
/// A care package containing protocol/procedure content that Freddy can route users to.
/// </summary>
public sealed class Package
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public ICollection<string> Tags { get; set; } = [];

    public ICollection<string> Synonyms { get; set; } = [];

    /// <summary>
    /// Categorises the package: Protocol (general), WorkInstruction (practical), PersonalPlan (client-specific).
    /// </summary>
    public PackageCategory Category { get; set; } = PackageCategory.Protocol;

    /// <summary>
    /// Required when <see cref="Category"/> is <see cref="PackageCategory.PersonalPlan"/>;
    /// must be null for Protocol and WorkInstruction packages.
    /// </summary>
    public Guid? ClientId { get; set; }

    public bool IsPublished { get; set; }

    public bool RequiresConfirmation { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<Document> Documents { get; set; } = [];

    /// <summary>
    /// Navigation property to the associated client (only for PersonalPlan packages).
    /// </summary>
    public Client? Client { get; set; }
}
