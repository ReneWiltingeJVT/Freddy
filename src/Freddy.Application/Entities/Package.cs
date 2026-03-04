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

    public bool IsPublished { get; set; }

    public bool RequiresConfirmation { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<Document> Documents { get; set; } = [];
}
