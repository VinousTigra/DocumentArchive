using DocumentArchive.Core.DTOs.ArchiveLog;
using DocumentArchive.Core.DTOs.Document;
using DocumentArchive.Core.DTOs.Shared;
using DocumentArchive.Core.DTOs.Statistics;

namespace DocumentArchive.Core.Interfaces.Services;

public interface IDocumentService
{
    Task<PagedResult<DocumentListItemDto>> GetDocumentsAsync(
        int page, int pageSize, string? search, Guid[]? categoryIds, Guid? userId,
        DateTime? fromDate, DateTime? toDate, string? sort,
        Guid currentUserId, List<string> permissions, CancellationToken cancellationToken);

    Task<DocumentResponseDto?> GetDocumentByIdAsync(Guid id, Guid currentUserId, List<string> permissions,
        CancellationToken cancellationToken);

    Task<DocumentResponseDto> CreateDocumentAsync(CreateDocumentDto createDto, Guid currentUserId,
        CancellationToken cancellationToken);

    Task UpdateDocumentAsync(Guid id, UpdateDocumentDto updateDto, Guid currentUserId, List<string> permissions,
        CancellationToken cancellationToken);

    Task DeleteDocumentAsync(Guid id, Guid currentUserId, List<string> permissions,
        CancellationToken cancellationToken);

    Task<PagedResult<ArchiveLogListItemDto>> GetDocumentLogsAsync(Guid documentId, int page, int pageSize,
        Guid currentUserId, List<string> permissions, CancellationToken cancellationToken);

    // Bulk-методы тоже нужно адаптировать, но пока можно оставить без изменений, если они используются только админами.
    // Для простоты добавим параметры и в них:
    Task<BulkOperationResult<Guid>> CreateBulkAsync(IEnumerable<CreateDocumentDto> createDtos, Guid currentUserId,
        List<string> permissions, CancellationToken cancellationToken);

    Task<BulkOperationResult<Guid>> UpdateBulkAsync(IEnumerable<UpdateBulkDocumentDto> updateDtos, Guid currentUserId,
        List<string> permissions, CancellationToken cancellationToken);

    Task<BulkOperationResult<Guid>> DeleteBulkAsync(IEnumerable<Guid> ids, Guid currentUserId, List<string> permissions,
        CancellationToken cancellationToken);

    // Статистические методы обычно доступны всем, но можно оставить без изменений или тоже с проверкой
    Task<Dictionary<string, int>> GetDocumentsCountByCategoryAsync(CancellationToken cancellationToken);
    Task<DocumentsStatisticsDto> GetDocumentsStatisticsAsync(CancellationToken cancellationToken);
}