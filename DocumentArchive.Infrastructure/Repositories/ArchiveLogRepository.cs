using DocumentArchive.Core.Models;

namespace DocumentArchive.Infrastructure.Repositories;

public class ArchiveLogRepository : FileStorageRepository<ArchiveLog>
{
    public ArchiveLogRepository() : base("logs.json", l => l.Id)
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