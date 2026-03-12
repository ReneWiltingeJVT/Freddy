using Freddy.Application.Entities;

namespace Freddy.Application.Common.Interfaces;

public interface IClientRepository
{
    Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Client>> GetAllAsync(bool? isActive, string? search, CancellationToken cancellationToken);

    Task<Client?> FindByAliasAsync(string alias, CancellationToken cancellationToken);

    Task<Client> CreateAsync(Client client, CancellationToken cancellationToken);

    Task<Client> UpdateAsync(Client client, CancellationToken cancellationToken);

    Task DeleteAsync(Client client, CancellationToken cancellationToken);
}
