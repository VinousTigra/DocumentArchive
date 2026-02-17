using DocumentArchive.Core.DTOs.Shared;
using DocumentArchive.Core.Interfaces.Repositorys;
using DocumentArchive.Core.Models;
using DocumentArchive.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

// для PagedResult

namespace DocumentArchive.Infrastructure.Repositories;

public class DocumentRepository : FileStorageRepository<Document>, IDocumentRepository
{
    public DocumentRepository(IOptions<StorageOptions> options)
        : base("documents.json", d => d.Id, options)
    {
    }

    // Для тестов можно оставить конструктор с dataDirectory
    public DocumentRepository(string? dataDirectory = null)
        : base("documents.json", d => d.Id, dataDirectory)
    {
    }

    public async Task<IEnumerable<Document>> GetByCategoryAsync(Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        return await FindAsync(d => d.CategoryId == categoryId, cancellationToken);
    }

    public async Task<IEnumerable<Document>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await FindAsync(d => d.UserId == userId, cancellationToken);
    }

    public async Task<PagedResult<Document>> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        IEnumerable<Guid>? categoryIds,
        Guid? userId,
        DateTime? fromDate,
        DateTime? toDate,
        string? sort,
        CancellationToken cancellationToken = default)
    {
        // Получаем все документы
        var documents = await GetAllAsync(cancellationToken);

        // ---- Фильтрация ----
        if (!string.IsNullOrWhiteSpace(search))
        {
            var lowerSearch = search.ToLowerInvariant();
            documents = documents.Where(d =>
                d.Title.ToLowerInvariant().Contains(lowerSearch) ||
                (d.Description?.ToLowerInvariant().Contains(lowerSearch) ?? false));
        }

        if (categoryIds != null && categoryIds.Any())
            documents = documents.Where(d => d.CategoryId.HasValue && categoryIds.Contains(d.CategoryId.Value));

        if (userId.HasValue)
            documents = documents.Where(d => d.UserId == userId);

        if (fromDate.HasValue)
            documents = documents.Where(d => d.UploadDate >= fromDate.Value);

        if (toDate.HasValue)
            documents = documents.Where(d => d.UploadDate <= toDate.Value);

        // ---- Сортировка ----
        IOrderedEnumerable<Document>? ordered = null;
        if (!string.IsNullOrWhiteSpace(sort))
        {
            var sortFields = sort.Split(',');
            foreach (var field in sortFields)
            {
                var parts = field.Split(':');
                var fieldName = parts[0].Trim().ToLowerInvariant();
                var direction = parts.Length > 1 ? parts[1].Trim().ToLowerInvariant() : "asc";

                Func<Document, object> keySelector = fieldName switch
                {
                    "title" => d => d.Title,
                    "uploaddate" => d => d.UploadDate,
                    "filename" => d => d.FileName,
                    _ => d => d.UploadDate
                };

                if (ordered == null)
                    ordered = direction == "asc"
                        ? documents.OrderBy(keySelector)
                        : documents.OrderByDescending(keySelector);
                else
                    ordered = direction == "asc"
                        ? ordered.ThenBy(keySelector)
                        : ordered.ThenByDescending(keySelector);
            }
        }

        // Если сортировка не указана – сортируем по дате загрузки по убыванию
        var sortedDocuments = ordered ?? documents.OrderByDescending(d => d.UploadDate);

        // ---- Пагинация ----
        var totalCount = documents.Count(); // количество после фильтрации
        var items = sortedDocuments
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<Document>
        {
            Items = items,
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}