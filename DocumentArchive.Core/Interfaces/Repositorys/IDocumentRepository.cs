using DocumentArchive.Core.DTOs.Shared;
using DocumentArchive.Core.Models;

namespace DocumentArchive.Core.Interfaces.Repositorys;

public interface IDocumentRepository
{
    Task<PagedResult<Document>> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        IEnumerable<Guid>? categoryIds,
        Guid? userId,
        DateTime? fromDate,
        DateTime? toDate,
        string? sort,
        CancellationToken cancellationToken = default);

    Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Document entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(Document entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Document>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Document>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
}