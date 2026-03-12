namespace Freddy.Application.Entities;

/// <summary>
/// A chunk of document or package content prepared for vector search (future RAG integration).
/// <para>
/// <b>Note:</b> This entity is defined for forward-compatibility only.
/// No database table or migration is created in the current MVP phase.
/// </para>
/// </summary>
public sealed class DocumentChunk
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }

    /// <summary>
    /// Denormalised for fast filtering — avoids joining through Document to Package.
    /// </summary>
    public Guid PackageId { get; set; }

    /// <summary>
    /// The text content of this chunk (typically 512 tokens with 50-token overlap).
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Zero-based index of this chunk within the source document.
    /// </summary>
    public int ChunkIndex { get; set; }

    // Future: vector(768) embedding via pgvector — not mapped in current phase.
    // public float[] Embedding { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; }

    public Document Document { get; set; } = null!;

    public Package Package { get; set; } = null!;
}
