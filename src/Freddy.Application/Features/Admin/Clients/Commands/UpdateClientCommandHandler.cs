using Freddy.Application.Common;
using Freddy.Application.Common.Interfaces;
using Freddy.Application.Entities;
using Freddy.Application.Features.Admin.Clients.DTOs;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Freddy.Application.Features.Admin.Clients.Commands;

public sealed class UpdateClientCommandHandler(
    IClientRepository clientRepository,
    ILogger<UpdateClientCommandHandler> logger) : IRequestHandler<UpdateClientCommand, Result<ClientDto>>
{
    public async Task<Result<ClientDto>> Handle(UpdateClientCommand request, CancellationToken cancellationToken)
    {
        Client? client = await clientRepository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (client is null)
        {
            return Result<ClientDto>.NotFound($"Client {request.Id} not found.");
        }

        client.DisplayName = request.DisplayName.Trim();
        client.Aliases = [.. request.Aliases.Select(a => a.Trim().ToLowerInvariant())];
        client.IsActive = request.IsActive;
        client.UpdatedAt = DateTimeOffset.UtcNow;

        _ = await clientRepository.UpdateAsync(client, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Client updated: {ClientId} — {DisplayName}", client.Id, client.DisplayName);

        return Result<ClientDto>.Success(MapToDto(client));
    }

    private static ClientDto MapToDto(Client c) => new(
        c.Id,
        c.DisplayName,
        c.Aliases.ToList().AsReadOnly(),
        c.IsActive,
        c.CreatedAt,
        c.UpdatedAt);
}
