using DocumentArchive.Core.DTOs.Shared;
using DocumentArchive.Core.Models;

namespace DocumentArchive.Core.Interfaces.Repositorys;

public interface ICategoryRepository
{
    Task<PagedResult<Category>> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        string? sortBy,
        string? sortOrder,
        CancellationToken cancellationToken = default);

    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Category?> FindByNameAsync(string name, CancellationToken cancellationToken = default);
    Task AddAsync(Category entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(Category entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}