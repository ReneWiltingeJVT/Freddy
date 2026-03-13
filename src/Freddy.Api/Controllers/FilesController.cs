using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Freddy.Api.Controllers;

/// <summary>
/// Serves uploaded and seeded files with proper content type and download headers.
/// </summary>
[ApiController]
[Route("api/files")]
[AllowAnonymous]
public sealed class FilesController(IWebHostEnvironment environment) : ControllerBase
{
    private static readonly string[] AllowedPrefixes =
    [
        "/uploads/",
        "/seed-documents/",
    ];

    private static readonly Dictionary<string, string> ContentTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".xls"] = "application/vnd.ms-excel",
        [".csv"] = "text/csv",
        [".doc"] = "application/msword",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".json"] = "application/json",
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
    };

    /// <summary>
    /// Downloads a file by its server-relative path with the correct content type and
    /// <c>Content-Disposition: attachment</c> header so browsers always save it instead
    /// of opening it inline.
    /// </summary>
    /// <param name="path">
    /// Server-relative path to the file, e.g. <c>/uploads/documents/xxx.pdf</c> or
    /// <c>/seed-documents/medicatieprotocol-handboek.pdf</c>.
    /// </param>
    [HttpGet("download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult Download([FromQuery] string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !IsAllowedPath(path))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid or disallowed file path.",
                Status = StatusCodes.Status400BadRequest,
            });
        }

        string webRoot = environment.WebRootPath;
        string relativePart = path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        string absolutePath = Path.GetFullPath(Path.Combine(webRoot, relativePart));

        // Security – prevent path traversal outside of wwwroot
        if (!absolutePath.StartsWith(webRoot, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid file path.",
                Status = StatusCodes.Status400BadRequest,
            });
        }

        if (!System.IO.File.Exists(absolutePath))
        {
            return NotFound(new ProblemDetails
            {
                Title = "File not found.",
                Status = StatusCodes.Status404NotFound,
            });
        }

        string extension = Path.GetExtension(absolutePath);
        string contentType = ContentTypeMap.TryGetValue(extension, out string? mapped)
            ? mapped
            : "application/octet-stream";

        string fileName = Path.GetFileName(absolutePath);

        // PhysicalFile with a fileDownloadName automatically adds
        // Content-Disposition: attachment; filename="..."
        return PhysicalFile(absolutePath, contentType, fileName);
    }

    private static bool IsAllowedPath(string path) =>
        AllowedPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
}
