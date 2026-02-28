using Freddy.Application.Common;
using Freddy.Application.Common.Interfaces;
using Freddy.Application.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Freddy.Application.Features.Admin.Documents.Commands;

public sealed class DeleteDocumentCommandHandler(
    IDocumentRepository documentRepository,
    ILogger<DeleteDocumentCommandHandler> logger) : IRequestHandler<DeleteDocumentCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteDocumentCommand request, CancellationToken cancellationToken)
    {
        Document? document = await documentRepository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (document is null || document.PackageId != request.PackageId)
        {
            return Result<bool>.NotFound($"Document {request.Id} not found.");
        }

        await documentRepository.DeleteAsync(document, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Document deleted: {DocumentId}", document.Id);

        return Result<bool>.Success(true);
    }
}
