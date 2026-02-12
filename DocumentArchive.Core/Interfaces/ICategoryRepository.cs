using DocumentArchive.Core.Models;

namespace DocumentArchive.Core.Interfaces;

public interface ICategoryRepository
{
    Task<IEnumerable<Category>> GetAllAsync();
    Task<Category?> GetByIdAsync(Guid id);
    Task AddAsync(Category entity);
    Task UpdateAsync(Category entity);
    Task DeleteAsync(Guid id);
    Task<IEnumerable<Category>> FindAsync(Func<Category, bool> predicate);
    Task<Category?> FindByNameAsync(string name);
}