namespace Freddy.Application.Entities;

public sealed class Package
{
    public Guid Id { get; set; }

    public required string Title { get; set; }

    public required string Description { get; set; }

    public required string Content { get; set; }

    public ICollection<string> Tags { get; set; } = [];

    public ICollection<string> Synonyms { get; set; } = [];

    public bool IsPublished { get; set; }

    public bool RequiresConfirmation { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<Document> Documents { get; set; } = [];
}
