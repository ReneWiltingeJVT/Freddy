using Freddy.Application.Common;
using Freddy.Application.Features.Admin.Clients.DTOs;
using MediatR;

namespace Freddy.Application.Features.Admin.Clients.Queries;

/// <summary>
/// Query to list all clients with optional filters.
/// </summary>
public sealed record ListClientsQuery(
    bool? IsActive = null,
    string? Search = null) : IRequest<Result<IReadOnlyList<ClientDto>>>;
