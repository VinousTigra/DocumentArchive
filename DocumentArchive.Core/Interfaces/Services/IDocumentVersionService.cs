using DocumentArchive.Core.DTOs.DocumentVersion;

namespace DocumentArchive.Core.Interfaces.Services;

public interface IDocumentVersionService
{
    Task<List<DocumentVersionListItemDto>> GetAllAsync(Guid? documentId = null,
        CancellationToken cancellationToken = default);

    Task<DocumentVersionResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<DocumentVersionResponseDto> CreateAsync(CreateDocumentVersionDto dto,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(Guid id, UpdateDocumentVersionDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}