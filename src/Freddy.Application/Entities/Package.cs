namespace Freddy.Application.Entities;

/// <summary>
/// A care package containing protocol/procedure content that Freddy can route users to.
/// </summary>
public sealed class Package
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public ICollection<string> Keywords { get; set; } = [];

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
