using DocumentArchive.Core.DTOs.Shared;
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

    // Для DI
    public CategoryRepository(IOptions<StorageOptions> options) 
        : base("categories.json", c => c.Id, options)
    {
    }

    public async Task<PagedResult<Category>> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        string? sortBy,
        string? sortOrder,
        CancellationToken cancellationToken = default)
    {
        var categories = await GetAllAsync(cancellationToken);

        // Фильтрация по поиску
        if (!string.IsNullOrWhiteSpace(search))
        {
            var lowerSearch = search.ToLowerInvariant();
            categories = categories.Where(c =>
                c.Name.ToLowerInvariant().Contains(lowerSearch) ||
                (c.Description?.ToLowerInvariant().Contains(lowerSearch) ?? false));
        }

        // Сортировка
        categories = (sortBy?.ToLowerInvariant()) switch
        {
            "name" => sortOrder == "asc"
                ? categories.OrderBy(c => c.Name)
                : categories.OrderByDescending(c => c.Name),
            "createdat" => sortOrder == "asc"
                ? categories.OrderBy(c => c.CreatedAt)
                : categories.OrderByDescending(c => c.CreatedAt),
            _ => categories.OrderBy(c => c.Name)
        };

        // Пагинация
        var enumerable = categories as Category[] ?? categories.ToArray();
        var totalCount = enumerable.Count();
        var items = enumerable
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<Category>
        {
            Items = items,
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<Category?> FindByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return (await FindAsync(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase), cancellationToken))
            .FirstOrDefault();
    }
}