using DocumentArchive.Core.DTOs.Shared;
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

    // Для DI
    public ArchiveLogRepository(IOptions<StorageOptions> options) 
        : base("logs.json", l => l.Id, options)
    {
    }

    public async Task<PagedResult<ArchiveLog>> GetPagedAsync(
        int page,
        int pageSize,
        Guid? documentId,
        Guid? userId,
        DateTime? fromDate,
        DateTime? toDate,
        ActionType? actionType,
        bool? isCritical,
        CancellationToken cancellationToken = default)
    {
        var logs = await GetAllAsync(cancellationToken);

        // Фильтрация
        if (documentId.HasValue)
            logs = logs.Where(l => l.DocumentId == documentId);
        if (userId.HasValue)
            logs = logs.Where(l => l.UserId == userId);
        if (fromDate.HasValue)
            logs = logs.Where(l => l.Timestamp >= fromDate.Value);
        if (toDate.HasValue)
            logs = logs.Where(l => l.Timestamp <= toDate.Value);
        if (actionType.HasValue)
            logs = logs.Where(l => l.ActionType == actionType.Value);
        if (isCritical.HasValue)
            logs = logs.Where(l => l.IsCritical == isCritical.Value);

        // Сортировка по дате (сначала новые)
        logs = logs.OrderByDescending(l => l.Timestamp);

        // Пагинация
        var totalCount = logs.Count();
        var items = logs
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<ArchiveLog>
        {
            Items = items,
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<IEnumerable<ArchiveLog>> GetByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        return await FindAsync(l => l.DocumentId == documentId, cancellationToken);
    }

    public async Task<IEnumerable<ArchiveLog>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await FindAsync(l => l.UserId == userId, cancellationToken);
    }
}