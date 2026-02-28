using Freddy.Application.Common;
using Freddy.Application.Common.Interfaces;
using Freddy.Application.Entities;
using Freddy.Application.Features.Admin.Documents.DTOs;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Freddy.Application.Features.Admin.Documents.Commands;

public sealed class CreateDocumentCommandHandler(
    IPackageRepository packageRepository,
    IDocumentRepository documentRepository,
    ILogger<CreateDocumentCommandHandler> logger) : IRequestHandler<CreateDocumentCommand, Result<DocumentDto>>
{
    public async Task<Result<DocumentDto>> Handle(CreateDocumentCommand request, CancellationToken cancellationToken)
    {
        Package? package = await packageRepository.GetByIdAsync(request.PackageId, cancellationToken).ConfigureAwait(false);
        if (package is null)
        {
            return Result<DocumentDto>.NotFound($"Package {request.PackageId} not found.");
        }

        if (!Enum.TryParse<DocumentType>(request.Type, ignoreCase: true, out DocumentType documentType))
        {
            return Result<DocumentDto>.ValidationError($"Invalid document type: {request.Type}. Valid types: Pdf, Steps, Link.");
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;

        var document = new Document
        {
            Id = Guid.CreateVersion7(),
            PackageId = request.PackageId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Type = documentType,
            StepsContent = request.StepsContent,
            FileUrl = request.FileUrl?.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };

        _ = await documentRepository.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Document created: {DocumentId} for package {PackageId}", document.Id, document.PackageId);

        return Result<DocumentDto>.Success(MapToDto(document));
    }

    private static DocumentDto MapToDto(Document d) => new(
        d.Id,
        d.PackageId,
        d.Name,
        d.Description,
        d.Type.ToString(),
        d.StepsContent,
        d.FileUrl,
        d.CreatedAt,
        d.UpdatedAt);
}
