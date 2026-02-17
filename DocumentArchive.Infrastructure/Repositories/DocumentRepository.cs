using DocumentArchive.Core.DTOs.Shared;
using DocumentArchive.Core.Interfaces.Repositorys;
using DocumentArchive.Core.Models;
using DocumentArchive.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace DocumentArchive.Infrastructure.Repositories;

public class DocumentRepository : FileStorageRepository<Document>, IDocumentRepository
{
    // Для DI (из appsettings)
    public DocumentRepository(IOptions<StorageOptions> options) 
        : base("documents.json", d => d.Id, options)
    {
    }

    // Для тестов
    public DocumentRepository(string? dataDirectory = null) 
        : base("documents.json", d => d.Id, dataDirectory)
    {
    }

    public Task<PagedResult<Document>> GetPagedAsync(int page, int pageSize, string? search,
        IEnumerable<Guid>? categoryIds, Guid? userId, DateTime? fromDate,
        DateTime? toDate, string? sort, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }




    public Task<IEnumerable<Document>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Document>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
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