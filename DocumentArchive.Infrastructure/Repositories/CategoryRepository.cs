using DocumentArchive.Core.Interfaces;
using DocumentArchive.Core.Models;

namespace DocumentArchive.Infrastructure.Repositories;

public class CategoryRepository : FileStorageRepository<Category>, ICategoryRepository
{
    public CategoryRepository(string? dataDirectory = null)
        : base("categories.json", c => c.Id, dataDirectory)
    {
    }

    public async Task<Category?> FindByNameAsync(string name)
    {
        return (await FindAsync(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase))).FirstOrDefault();
    }
}