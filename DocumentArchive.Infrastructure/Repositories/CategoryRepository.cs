using DocumentArchive.Core.Models;

namespace DocumentArchive.Infrastructure.Repositories;

public class CategoryRepository : FileStorageRepository<Category>
{
    public CategoryRepository() : base("categories.json", c => c.Id)
    {
    }

    public async Task<Category?> FindByNameAsync(string name)
    {
        return (await FindAsync(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase))).FirstOrDefault();
    }
}