using DocumentArchive.Core.DTOs.ArchiveLog;
using DocumentArchive.Core.DTOs.Shared;
using DocumentArchive.Core.Models;

namespace DocumentArchive.Core.Interfaces.Services;

public interface IArchiveLogService
{
    Task<PagedResult<ArchiveLogListItemDto>> GetLogsAsync(
        int page,
        int pageSize,
        Guid? documentId,
        Guid? userId,
        DateTime? fromDate,
        DateTime? toDate,
        ActionType? actionType,
        bool? isCritical,
        CancellationToken cancellationToken = default);

    Task<ArchiveLogResponseDto?> GetLogByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ArchiveLogResponseDto> CreateLogAsync(CreateArchiveLogDto createDto, CancellationToken cancellationToken = default);
    Task DeleteLogAsync(Guid id, CancellationToken cancellationToken = default);
}