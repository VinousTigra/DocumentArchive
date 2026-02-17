using DocumentArchive.Core.Interfaces;
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

    public async Task<IEnumerable<ArchiveLog>> GetByDocumentIdAsync(Guid documentId)
    {
        return await FindAsync(l => l.DocumentId == documentId);
    }

    public async Task<IEnumerable<ArchiveLog>> GetByUserIdAsync(Guid userId)
    {
        return await FindAsync(l => l.UserId == userId);
    }
}