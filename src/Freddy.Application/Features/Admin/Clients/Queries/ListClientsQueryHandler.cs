using Freddy.Application.Common;
using Freddy.Application.Common.Interfaces;
using Freddy.Application.Entities;
using Freddy.Application.Features.Admin.Clients.DTOs;
using MediatR;

namespace Freddy.Application.Features.Admin.Clients.Queries;

public sealed class ListClientsQueryHandler(
    IClientRepository clientRepository) : IRequestHandler<ListClientsQuery, Result<IReadOnlyList<ClientDto>>>
{
    public async Task<Result<IReadOnlyList<ClientDto>>> Handle(
        ListClientsQuery request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Client> clients = await clientRepository
            .GetAllAsync(request.IsActive, request.Search, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<ClientDto> dtos = clients
            .Select(MapToDto)
            .ToList()
            .AsReadOnly();

        return Result<IReadOnlyList<ClientDto>>.Success(dtos);
    }

    private static ClientDto MapToDto(Client c) => new(
        c.Id,
        c.DisplayName,
        c.Aliases.ToList().AsReadOnly(),
        c.IsActive,
        c.CreatedAt,
        c.UpdatedAt);
}
