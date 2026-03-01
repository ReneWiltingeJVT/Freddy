namespace Freddy.Application.Common.Interfaces;

/// <summary>
/// Abstracts file storage for document uploads.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Uploads a file and returns the relative URL where it can be accessed.
    /// </summary>
    Task<string> UploadAsync(Stream fileStream, string fileName, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a previously uploaded file by its relative URL.
    /// </summary>
    Task DeleteAsync(string fileUrl, CancellationToken cancellationToken);
}
