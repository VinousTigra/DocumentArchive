using DocumentArchive.Core.Models;

namespace DocumentArchive.Core.Interfaces;

public interface IDocumentRepository
{
    Task<IEnumerable<Document>> GetAllAsync();
    Task<Document?> GetByIdAsync(Guid id);
    Task AddAsync(Document entity);
    Task UpdateAsync(Document entity);
    Task DeleteAsync(Guid id);
    Task<IEnumerable<Document>> FindAsync(Func<Document, bool> predicate);
    Task<IEnumerable<Document>> GetByCategoryAsync(Guid categoryId);
    Task<IEnumerable<Document>> GetByUserAsync(Guid userId);
    Task<IEnumerable<Document>> SearchAsync(string searchTerm);
}