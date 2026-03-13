using Freddy.Application.Common;
using Freddy.Application.Features.Admin.Clients.DTOs;
using MediatR;

namespace Freddy.Application.Features.Admin.Clients.Commands;

/// <summary>
/// Command to create a new client.
/// </summary>
public sealed record CreateClientCommand(
    string DisplayName,
    IReadOnlyList<string> Aliases) : IRequest<Result<ClientDto>>;
