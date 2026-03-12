using Freddy.Application.Common;
using Freddy.Application.Common.Interfaces;
using Freddy.Application.Entities;
using Freddy.Application.Features.Admin.Clients.DTOs;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Freddy.Application.Features.Admin.Clients.Commands;

public sealed class CreateClientCommandHandler(
    IClientRepository clientRepository,
    ILogger<CreateClientCommandHandler> logger) : IRequestHandler<CreateClientCommand, Result<ClientDto>>
{
    public async Task<Result<ClientDto>> Handle(CreateClientCommand request, CancellationToken cancellationToken)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        var client = new Client
        {
            Id = Guid.CreateVersion7(),
            DisplayName = request.DisplayName.Trim(),
            Aliases = request.Aliases.Select(a => a.Trim().ToLowerInvariant()).ToList(),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _ = await clientRepository.CreateAsync(client, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Client created: {ClientId} — {DisplayName}", client.Id, client.DisplayName);

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
