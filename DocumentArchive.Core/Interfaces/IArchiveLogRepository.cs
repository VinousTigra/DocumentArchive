using DocumentArchive.Core.Models;

namespace DocumentArchive.Core.Interfaces;

public interface IArchiveLogRepository
{
    Task<IEnumerable<ArchiveLog>> GetAllAsync();
    Task<ArchiveLog?> GetByIdAsync(Guid id);
    Task AddAsync(ArchiveLog entity);
    Task UpdateAsync(ArchiveLog entity);
    Task DeleteAsync(Guid id);
    Task<IEnumerable<ArchiveLog>> FindAsync(Func<ArchiveLog, bool> predicate);
    Task<IEnumerable<ArchiveLog>> GetByDocumentIdAsync(Guid documentId);
    Task<IEnumerable<ArchiveLog>> GetByUserIdAsync(Guid userId);
}