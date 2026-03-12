using Freddy.Application.Common;
using Freddy.Application.Features.Admin.Clients.DTOs;
using MediatR;

namespace Freddy.Application.Features.Admin.Clients.Queries;

/// <summary>
/// Query to get a client by ID.
/// </summary>
public sealed record GetClientQuery(Guid Id) : IRequest<Result<ClientDto>>;
