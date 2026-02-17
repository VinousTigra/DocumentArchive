using DocumentArchive.Core.DTOs.Shared;
using DocumentArchive.Core.Interfaces;
using DocumentArchive.Core.Interfaces.Repositorys;
using DocumentArchive.Core.Models;
using DocumentArchive.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace DocumentArchive.Infrastructure.Repositories;

public class CategoryRepository : FileStorageRepository<Category>, ICategoryRepository
{
    // Для тестов
    public CategoryRepository(string? dataDirectory = null) 
        : base("categories.json", c => c.Id, dataDirectory)
    {
    }

    // 🔸 Для DI
    public CategoryRepository(IOptions<StorageOptions> options) 
        : base("categories.json", c => c.Id, options)
    {
    }

    public Task<PagedResult<Category>> GetPagedAsync(int page, int pageSize, string? search, string? sortBy, string? sortOrder)
    {
        throw new NotImplementedException();
    }

    public async Task<Category?> FindByNameAsync(string name)
    {
        return (await FindAsync(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase))).FirstOrDefault();
    }

    public Task<PagedResult<Category>> GetPagedAsync(int page, int pageSize, string? search, string? sortBy, string? sortOrder,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Category?> FindByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task AddAsync(Category entity, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(Category entity, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}