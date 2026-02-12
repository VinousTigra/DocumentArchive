using DocumentArchive.Core.Interfaces;
using DocumentArchive.Core.Models;

namespace DocumentArchive.Infrastructure.Repositories;

public class ArchiveLogRepository : FileStorageRepository<ArchiveLog>, IArchiveLogRepository
{
    public ArchiveLogRepository(string? dataDirectory = null)
        : base("logs.json", l => l.Id, dataDirectory)
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