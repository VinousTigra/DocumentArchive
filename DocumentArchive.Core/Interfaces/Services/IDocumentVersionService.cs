using DocumentArchive.Core.DTOs.DocumentVersion;

namespace DocumentArchive.Core.Interfaces.Services;

public interface IDocumentVersionService
{
    Task<List<DocumentVersionListItemDto>> GetAllAsync(Guid? documentId, Guid currentUserId, List<string> permissions,
        CancellationToken cancellationToken);

    Task<DocumentVersionResponseDto?> GetByIdAsync(Guid id, Guid currentUserId, List<string> permissions,
        CancellationToken cancellationToken);

    Task<DocumentVersionResponseDto> CreateAsync(CreateDocumentVersionDto dto, Guid currentUserId,
        CancellationToken cancellationToken); // создание доступно владельцу или админу

    Task UpdateAsync(Guid id, UpdateDocumentVersionDto dto, Guid currentUserId, List<string> permissions,
        CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, Guid currentUserId, List<string> permissions, CancellationToken cancellationToken);
}