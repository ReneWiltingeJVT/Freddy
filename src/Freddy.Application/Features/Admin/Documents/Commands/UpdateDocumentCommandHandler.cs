using Freddy.Application.Common;
using Freddy.Application.Common.Interfaces;
using Freddy.Application.Entities;
using Freddy.Application.Features.Admin.Documents.DTOs;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Freddy.Application.Features.Admin.Documents.Commands;

public sealed class UpdateDocumentCommandHandler(
    IDocumentRepository documentRepository,
    ILogger<UpdateDocumentCommandHandler> logger) : IRequestHandler<UpdateDocumentCommand, Result<DocumentDto>>
{
    public async Task<Result<DocumentDto>> Handle(UpdateDocumentCommand request, CancellationToken cancellationToken)
    {
        Document? document = await documentRepository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (document is null || document.PackageId != request.PackageId)
        {
            return Result<DocumentDto>.NotFound($"Document {request.Id} not found.");
        }

        if (!Enum.TryParse<DocumentType>(request.Type, ignoreCase: true, out DocumentType documentType))
        {
            return Result<DocumentDto>.ValidationError($"Invalid document type: {request.Type}. Valid types: Pdf, Steps, Link.");
        }

        document.Name = request.Name.Trim();
        document.Description = request.Description?.Trim();
        document.Type = documentType;
        document.StepsContent = request.StepsContent;
        document.FileUrl = request.FileUrl?.Trim();
        document.UpdatedAt = DateTimeOffset.UtcNow;

        _ = await documentRepository.UpdateAsync(document, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Document updated: {DocumentId}", document.Id);

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
