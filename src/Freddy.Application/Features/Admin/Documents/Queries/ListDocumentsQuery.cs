using Freddy.Application.Common;
using Freddy.Application.Features.Admin.Documents.DTOs;
using MediatR;

namespace Freddy.Application.Features.Admin.Documents.Queries;

/// <summary>
/// Query to list all documents for a specific package.
/// </summary>
public sealed record ListDocumentsQuery(Guid PackageId) : IRequest<Result<IReadOnlyList<DocumentDto>>>;
