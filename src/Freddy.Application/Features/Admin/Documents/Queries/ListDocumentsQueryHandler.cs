using Freddy.Application.Common;
using Freddy.Application.Common.Interfaces;
using Freddy.Application.Entities;
using Freddy.Application.Features.Admin.Documents.DTOs;
using MediatR;

namespace Freddy.Application.Features.Admin.Documents.Queries;

public sealed class ListDocumentsQueryHandler(
    IPackageRepository packageRepository,
    IDocumentRepository documentRepository) : IRequestHandler<ListDocumentsQuery, Result<IReadOnlyList<DocumentDto>>>
{
    public async Task<Result<IReadOnlyList<DocumentDto>>> Handle(
        ListDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        Package? package = await packageRepository.GetByIdAsync(request.PackageId, cancellationToken).ConfigureAwait(false);
        if (package is null)
        {
            return Result<IReadOnlyList<DocumentDto>>.NotFound($"Package {request.PackageId} not found.");
        }

        IReadOnlyList<Document> documents = await documentRepository
            .GetByPackageIdAsync(request.PackageId, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<DocumentDto> dtos = documents
            .Select(MapToDto)
            .ToList()
            .AsReadOnly();

        return Result<IReadOnlyList<DocumentDto>>.Success(dtos);
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
