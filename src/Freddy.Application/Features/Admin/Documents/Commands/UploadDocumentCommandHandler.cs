using Freddy.Application.Common;
using Freddy.Application.Common.Interfaces;
using Freddy.Application.Entities;
using Freddy.Application.Features.Admin.Documents.DTOs;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Freddy.Application.Features.Admin.Documents.Commands;

public sealed class UploadDocumentCommandHandler(
    IPackageRepository packageRepository,
    IDocumentRepository documentRepository,
    IFileStorageService fileStorageService,
    ILogger<UploadDocumentCommandHandler> logger) : IRequestHandler<UploadDocumentCommand, Result<DocumentDto>>
{
    public async Task<Result<DocumentDto>> Handle(UploadDocumentCommand request, CancellationToken cancellationToken)
    {
        Package? package = await packageRepository.GetByIdAsync(request.PackageId, cancellationToken).ConfigureAwait(false);
        if (package is null)
        {
            return Result<DocumentDto>.NotFound($"Package {request.PackageId} not found.");
        }

        if (!Enum.TryParse(request.Type, ignoreCase: true, out DocumentType documentType))
        {
            return Result<DocumentDto>.ValidationError($"Invalid document type: {request.Type}. Valid types: Pdf, Steps, Link.");
        }

        // Upload file to storage
        string fileUrl = await fileStorageService.UploadAsync(
            request.FileStream, request.FileName, cancellationToken).ConfigureAwait(false);

        // Determine a display name from the file name (strip extension)
        string displayName = Path.GetFileNameWithoutExtension(request.FileName).Trim();
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = request.FileName;
        }

        var document = new Document
        {
            Id = Guid.CreateVersion7(),
            PackageId = request.PackageId,
            Name = displayName,
            Description = request.Description?.Trim(),
            Type = documentType,
            FileUrl = fileUrl,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        _ = await documentRepository.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Document uploaded for package {PackageId}: {DocumentId} ({FileName})",
            request.PackageId, document.Id, request.FileName);

        return Result<DocumentDto>.Success(new DocumentDto(
            document.Id,
            document.PackageId,
            document.Name,
            document.Description,
            document.Type.ToString(),
            document.StepsContent,
            document.FileUrl,
            document.CreatedAt,
            document.UpdatedAt));
    }
}
