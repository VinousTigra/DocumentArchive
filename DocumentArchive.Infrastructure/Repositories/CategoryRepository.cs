using DocumentArchive.Core.Interfaces;
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

    public async Task<Category?> FindByNameAsync(string name)
    {
        return (await FindAsync(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase))).FirstOrDefault();
    }
}