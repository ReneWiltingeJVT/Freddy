using Freddy.Application.Common;
using Freddy.Application.Features.Admin.Documents.DTOs;
using MediatR;

namespace Freddy.Application.Features.Admin.Documents.Commands;

/// <summary>
/// Command to update an existing document.
/// </summary>
public sealed record UpdateDocumentCommand(
    Guid PackageId,
    Guid Id,
    string Name,
    string? Description,
    string Type,
    string? StepsContent,
    string? FileUrl) : IRequest<Result<DocumentDto>>;
