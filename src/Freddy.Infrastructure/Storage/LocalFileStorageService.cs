using Freddy.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Freddy.Infrastructure.Storage;

/// <summary>
/// Stores uploaded files on the local filesystem under wwwroot/uploads/documents.
/// </summary>
public sealed class LocalFileStorageService(
    string webRootPath,
    ILogger<LocalFileStorageService> logger) : IFileStorageService
{
    private const string UploadFolder = "uploads/documents";

    public async Task<string> UploadAsync(Stream fileStream, string fileName, CancellationToken cancellationToken)
    {
        string sanitizedName = SanitizeFileName(fileName);
        string uniqueName = $"{Guid.CreateVersion7()}-{sanitizedName}";
        string directoryPath = Path.Combine(webRootPath, UploadFolder);

        Directory.CreateDirectory(directoryPath);

        string filePath = Path.Combine(directoryPath, uniqueName);

        FileStream output = new(filePath, FileMode.Create, FileAccess.Write);
        await using (output.ConfigureAwait(false))
        {
            await fileStream.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
        }

        string relativeUrl = $"/{UploadFolder}/{uniqueName}";
        logger.LogInformation("File uploaded: {FileName} → {RelativeUrl}", fileName, relativeUrl);

        return relativeUrl;
    }

    public Task DeleteAsync(string fileUrl, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
        {
            return Task.CompletedTask;
        }

        // Convert relative URL to filesystem path
        string relativePath = fileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        string filePath = Path.Combine(webRootPath, relativePath);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            logger.LogInformation("File deleted: {FileUrl}", fileUrl);
        }

        return Task.CompletedTask;
    }

    private static string SanitizeFileName(string fileName)
    {
        char[] invalid = [.. Path.GetInvalidFileNameChars()];
        string sanitized = string.Concat(fileName
            .Select(c => invalid.Contains(c) ? '_' : c));

        return sanitized.Length > 100 ? sanitized[..100] : sanitized;
    }
}
