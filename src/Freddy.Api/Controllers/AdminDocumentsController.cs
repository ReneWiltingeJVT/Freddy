using Freddy.Api.Extensions;
using Freddy.Api.Models;
using Freddy.Application.Common;
using Freddy.Application.Features.Admin.Documents.Commands;
using Freddy.Application.Features.Admin.Documents.DTOs;
using Freddy.Application.Features.Admin.Documents.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Freddy.Api.Controllers;

/// <summary>
/// Admin API for managing package documents.
/// </summary>
[ApiController]
[Route("api/admin/packages/{packageId:guid}/documents")]
[AllowAnonymous]
[Produces("application/json")]
public sealed class AdminDocumentsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Lists all documents for a package.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<DocumentDto>>> ListAsync(
        Guid packageId,
        CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<DocumentDto>> result = await mediator.Send(
            new ListDocumentsQuery(packageId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Creates a new document for a package.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentDto>> CreateAsync(
        Guid packageId,
        [FromBody] CreateDocumentRequest request,
        CancellationToken cancellationToken)
    {
        Result<DocumentDto> result = await mediator.Send(
            new CreateDocumentCommand(
                packageId,
                request.Name,
                request.Description,
                request.Type,
                request.StepsContent,
                request.FileUrl),
            cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction("List", new { packageId }, result.Value)
            : result.ToActionResult();
    }

    /// <summary>
    /// Uploads a file and creates a document record.
    /// </summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50 MB
    public async Task<ActionResult<DocumentDto>> UploadAsync(
        Guid packageId,
        IFormFile file,
        [FromForm] string? description,
        [FromForm] string? type,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new ProblemDetails { Title = "No file uploaded.", Status = 400 });
        }

        // Auto-detect document type from extension if not provided
        string documentType = type ?? DetectDocumentType(file.FileName);

        await using Stream stream = file.OpenReadStream();
        Result<DocumentDto> result = await mediator.Send(
            new UploadDocumentCommand(
                packageId,
                file.FileName,
                description,
                documentType,
                stream),
            cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction("List", new { packageId }, result.Value)
            : result.ToActionResult();
    }

    /// <summary>
    /// Updates an existing document.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentDto>> UpdateAsync(
        Guid packageId,
        Guid id,
        [FromBody] UpdateDocumentRequest request,
        CancellationToken cancellationToken)
    {
        Result<DocumentDto> result = await mediator.Send(
            new UpdateDocumentCommand(
                packageId,
                id,
                request.Name,
                request.Description,
                request.Type,
                request.StepsContent,
                request.FileUrl),
            cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>
    /// Deletes a document.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(
        Guid packageId,
        Guid id,
        CancellationToken cancellationToken)
    {
        Result<bool> result = await mediator.Send(
            new DeleteDocumentCommand(packageId, id), cancellationToken);

        return result.IsSuccess ? NoContent() : result.ToActionResult();
    }

    private static string DetectDocumentType(string fileName)
    {
        string extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "Pdf",
            ".xlsx" or ".xls" or ".csv" => "Link",
            ".doc" or ".docx" => "Pdf",
            _ => "Link",
        };
    }
}
