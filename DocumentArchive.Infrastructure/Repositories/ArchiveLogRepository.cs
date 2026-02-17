using DocumentArchive.Core.DTOs.Shared;
using DocumentArchive.Core.Interfaces;
using DocumentArchive.Core.Interfaces.Repositorys;
using DocumentArchive.Core.Models;
using DocumentArchive.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace DocumentArchive.Infrastructure.Repositories;

public class ArchiveLogRepository : FileStorageRepository<ArchiveLog>, IArchiveLogRepository
{
    // Для тестов
    public ArchiveLogRepository(string? dataDirectory = null) 
        : base("logs.json", l => l.Id, dataDirectory)
    {
    }

    // 🔸 Для DI
    public ArchiveLogRepository(IOptions<StorageOptions> options) 
        : base("logs.json", l => l.Id, options)
    {
    }

    public Task<PagedResult<ArchiveLog>> GetPagedAsync(int page, int pageSize, Guid? documentId, Guid? userId, DateTime? fromDate, DateTime? toDate,
        ActionType? actionType, bool? isCritical)
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<ArchiveLog>> GetByDocumentIdAsync(Guid documentId)
    {
        return await FindAsync(l => l.DocumentId == documentId);
    }

    public async Task<IEnumerable<ArchiveLog>> GetByUserIdAsync(Guid userId)
    {
        return await FindAsync(l => l.UserId == userId);
    }

    public Task<PagedResult<ArchiveLog>> GetPagedAsync(int page, int pageSize, Guid? documentId, Guid? userId, DateTime? fromDate, DateTime? toDate,
        ActionType? actionType, bool? isCritical, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<ArchiveLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task AddAsync(ArchiveLog entity, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<ArchiveLog>> GetByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<ArchiveLog>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}