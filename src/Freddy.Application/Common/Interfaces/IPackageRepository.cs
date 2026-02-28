using Freddy.Application.Entities;

namespace Freddy.Application.Common.Interfaces;

public interface IPackageRepository
{
    Task<IReadOnlyList<Package>> SearchAsync(string query, CancellationToken cancellationToken);

    Task<Package?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Package>> GetAllActiveAsync(CancellationToken cancellationToken);
}
