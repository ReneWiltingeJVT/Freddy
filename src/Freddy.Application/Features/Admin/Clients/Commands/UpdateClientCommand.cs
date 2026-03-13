using Freddy.Application.Common;
using Freddy.Application.Features.Admin.Clients.DTOs;
using MediatR;

namespace Freddy.Application.Features.Admin.Clients.Commands;

/// <summary>
/// Command to update an existing client.
/// </summary>
public sealed record UpdateClientCommand(
    Guid Id,
    string DisplayName,
    IReadOnlyList<string> Aliases,
    bool IsActive) : IRequest<Result<ClientDto>>;
