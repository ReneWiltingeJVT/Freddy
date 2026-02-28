namespace Freddy.Application.Entities;

/// <summary>
/// A document attached to a package, providing supplementary content like PDFs, step-by-step instructions, or links.
/// </summary>
public sealed class Document
{
    public Guid Id { get; set; }

    public Guid PackageId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DocumentType Type { get; set; }

    /// <summary>
    /// JSON content for step-by-step instructions (when Type is Steps).
    /// </summary>
    public string? StepsContent { get; set; }

    /// <summary>
    /// URL for PDF files or external links (when Type is Pdf or Link).
    /// </summary>
    public string? FileUrl { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Package Package { get; set; } = null!;
}
