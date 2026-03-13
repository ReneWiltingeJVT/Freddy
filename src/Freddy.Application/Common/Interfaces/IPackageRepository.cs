using Freddy.Application.Entities;

namespace Freddy.Application.Common.Interfaces;

public interface IPackageRepository
{
    Task<IReadOnlyList<Package>> SearchAsync(string query, CancellationToken cancellationToken);

    Task<Package?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Package>> GetAllPublishedAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<Package>> GetPublishedByClientIdAsync(Guid clientId, CancellationToken cancellationToken);

    /// <summary>Returns all published packages filtered by category, ordered by title.</summary>
    Task<IReadOnlyList<Package>> GetAllPublishedByCategoryAsync(
        PackageCategory category,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Package>> GetAllAsync(bool? isPublished, string? search, PackageCategory? category, CancellationToken cancellationToken);

    Task<Package> CreateAsync(Package package, CancellationToken cancellationToken);

    Task<Package> UpdateAsync(Package package, CancellationToken cancellationToken);

    Task DeleteAsync(Package package, CancellationToken cancellationToken);
}
