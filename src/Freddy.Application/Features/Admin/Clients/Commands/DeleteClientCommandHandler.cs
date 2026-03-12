using Freddy.Application.Common;
using Freddy.Application.Common.Interfaces;
using Freddy.Application.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Freddy.Application.Features.Admin.Clients.Commands;

public sealed class DeleteClientCommandHandler(
    IClientRepository clientRepository,
    ILogger<DeleteClientCommandHandler> logger) : IRequestHandler<DeleteClientCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteClientCommand request, CancellationToken cancellationToken)
    {
        Client? client = await clientRepository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (client is null)
        {
            return Result<bool>.NotFound($"Client {request.Id} not found.");
        }

        await clientRepository.DeleteAsync(client, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Client deleted: {ClientId} — {DisplayName}", client.Id, client.DisplayName);

        return Result<bool>.Success(true);
    }
}
