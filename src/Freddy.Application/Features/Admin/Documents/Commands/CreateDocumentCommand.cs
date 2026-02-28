using Freddy.Application.Common;
using Freddy.Application.Features.Admin.Documents.DTOs;
using MediatR;

namespace Freddy.Application.Features.Admin.Documents.Commands;

/// <summary>
/// Command to create a new document for a package.
/// </summary>
public sealed record CreateDocumentCommand(
    Guid PackageId,
    string Name,
    string? Description,
    string Type,
    string? StepsContent,
    string? FileUrl) : IRequest<Result<DocumentDto>>;
