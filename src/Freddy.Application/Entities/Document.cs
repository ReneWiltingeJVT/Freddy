namespace Freddy.Application.Entities;

public sealed class Document
{
    public Guid Id { get; set; }

    public Guid PackageId { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public DocumentType Type { get; set; }

    public string? StepsContent { get; set; }

    public string? FileUrl { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Package Package { get; set; } = null!;
}
