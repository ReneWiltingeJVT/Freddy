using Freddy.Application.Common;
using Freddy.Application.Features.Admin.Documents.DTOs;
using MediatR;

namespace Freddy.Application.Features.Admin.Documents.Commands;

/// <summary>
/// Command to upload a file and create a document record for a package.
/// </summary>
public sealed record UploadDocumentCommand(
    Guid PackageId,
    string FileName,
    string? Description,
    string Type,
    Stream FileStream) : IRequest<Result<DocumentDto>>;
