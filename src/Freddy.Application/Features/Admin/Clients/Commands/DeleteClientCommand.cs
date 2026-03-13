using Freddy.Application.Common;
using MediatR;

namespace Freddy.Application.Features.Admin.Clients.Commands;

/// <summary>
/// Command to delete a client.
/// </summary>
public sealed record DeleteClientCommand(Guid Id) : IRequest<Result<bool>>;
