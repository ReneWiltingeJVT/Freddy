namespace Freddy.Application.Entities;

/// <summary>
/// The type of document content.
/// </summary>
public enum DocumentType
{
    /// <summary>A PDF file referenced by URL.</summary>
    Pdf = 0,

    /// <summary>Step-by-step instructions stored as JSON.</summary>
    Steps = 1,

    /// <summary>An external link/URL.</summary>
    Link = 2,
}
