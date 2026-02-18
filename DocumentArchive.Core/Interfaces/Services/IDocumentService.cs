using DocumentArchive.Core.DTOs.ArchiveLog;
using DocumentArchive.Core.DTOs.Document;
using DocumentArchive.Core.DTOs.Shared;

namespace DocumentArchive.Core.Interfaces.Services;

public interface IDocumentService
{
    Task<PagedResult<DocumentListItemDto>> GetDocumentsAsync(
        int page,
        int pageSize,
        string? search,
        Guid[]? categoryIds,           
        Guid? userId,
        DateTime? fromDate,
        DateTime? toDate,
        string? sort,
        CancellationToken cancellationToken = default); 

    Task<DocumentResponseDto?> GetDocumentByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DocumentResponseDto> CreateDocumentAsync(CreateDocumentDto createDto, CancellationToken cancellationToken = default);
    Task UpdateDocumentAsync(Guid id, UpdateDocumentDto updateDto, CancellationToken cancellationToken = default);
    Task DeleteDocumentAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task<PagedResult<ArchiveLogListItemDto>> GetDocumentLogsAsync(
        Guid documentId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<BulkOperationResult<Guid>> CreateBulkAsync(
        IEnumerable<CreateDocumentDto> createDtos,
        CancellationToken cancellationToken = default);

    Task<BulkOperationResult<Guid>> UpdateBulkAsync(
        IEnumerable<UpdateBulkDocumentDto> updateDtos,
        CancellationToken cancellationToken = default);

    Task<BulkOperationResult<Guid>> DeleteBulkAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default);
}