using DocumentArchive.Core.Interfaces;
using DocumentArchive.Core.Models;

namespace DocumentArchive.Infrastructure.Repositories;

public class DocumentRepository : FileStorageRepository<Document>, IDocumentRepository
{
    public DocumentRepository(string? dataDirectory = null)
        : base("documents.json", d => d.Id, dataDirectory)
    {
    }

    public async Task<IEnumerable<Document>> GetByCategoryAsync(Guid categoryId)
    {
        return await FindAsync(d => d.CategoryId == categoryId);
    }

    public async Task<IEnumerable<Document>> GetByUserAsync(Guid userId)
    {
        return await FindAsync(d => d.UserId == userId);
    }

    public async Task<IEnumerable<Document>> SearchAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return await GetAllAsync();

        searchTerm = searchTerm.ToLowerInvariant();
        return await FindAsync(d =>
            d.Title.ToLowerInvariant().Contains(searchTerm) ||
            (d.Description != null && d.Description.ToLowerInvariant().Contains(searchTerm)));
    }
}