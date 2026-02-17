using DocumentArchive.Core.DTOs.Shared;
using DocumentArchive.Core.Models;

namespace DocumentArchive.Core.Interfaces.Repositorys;

public interface IArchiveLogRepository
{
    Task<PagedResult<ArchiveLog>> GetPagedAsync(
        int page,
        int pageSize,
        Guid? documentId,
        Guid? userId,
        DateTime? fromDate,
        DateTime? toDate,
        ActionType? actionType,
        bool? isCritical,
        CancellationToken cancellationToken = default);

    Task<ArchiveLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(ArchiveLog entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ArchiveLog>> GetByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ArchiveLog>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}