using Freddy.Application.Common;
using MediatR;

namespace Freddy.Application.Features.Admin.Documents.Commands;

/// <summary>
/// Command to delete a document.
/// </summary>
public sealed record DeleteDocumentCommand(Guid PackageId, Guid Id) : IRequest<Result<bool>>;
