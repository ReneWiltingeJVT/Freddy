using Freddy.Application.Common;
using Freddy.Application.Common.Interfaces;
using Freddy.Application.Entities;
using Freddy.Application.Features.Admin.Clients.DTOs;
using MediatR;

namespace Freddy.Application.Features.Admin.Clients.Queries;

public sealed class GetClientQueryHandler(
    IClientRepository clientRepository) : IRequestHandler<GetClientQuery, Result<ClientDto>>
{
    public async Task<Result<ClientDto>> Handle(GetClientQuery request, CancellationToken cancellationToken)
    {
        Client? client = await clientRepository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (client is null)
        {
            return Result<ClientDto>.NotFound($"Client {request.Id} not found.");
        }

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
