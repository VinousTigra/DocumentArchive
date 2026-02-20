using AutoMapper;
using AutoMapper.QueryableExtensions;
using DocumentArchive.Core.DTOs.ArchiveLog;
using DocumentArchive.Core.DTOs.Document;
using DocumentArchive.Core.DTOs.Shared;
using DocumentArchive.Core.DTOs.Statistics;
using DocumentArchive.Core.Interfaces.Services;
using DocumentArchive.Core.Models;
using DocumentArchive.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocumentArchive.Services.Services;

public class DocumentService : IDocumentService
{
    private readonly AppDbContext _context;
    private readonly ILogger<DocumentService> _logger;
    private readonly IMapper _mapper;

    public DocumentService(AppDbContext context, IMapper mapper, ILogger<DocumentService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResult<DocumentListItemDto>> GetDocumentsAsync(
        int page,
        int pageSize,
        string? search,
        Guid[]? categoryIds,
        Guid? userId,
        DateTime? fromDate,
        DateTime? toDate,
        string? sort,
        Guid currentUserId,
        List<string> permissions,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting documents page {Page} size {PageSize} for user {UserId}", page, pageSize,
            currentUserId);

        var query = _context.Documents
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(d => d.Title.Contains(search) ||
                                     (d.Description != null && d.Description.Contains(search)));

        if (categoryIds?.Any() == true)
            query = query.Where(d => d.CategoryId.HasValue && categoryIds.Contains(d.CategoryId.Value));

        if (fromDate.HasValue)
            query = query.Where(d => d.UploadDate >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(d => d.UploadDate <= toDate.Value);

        if (!permissions.Contains("ViewAnyDocument"))
            query = query.Where(d => d.UserId == currentUserId);
        else if (userId.HasValue) query = query.Where(d => d.UserId == userId.Value);

        query = ApplySorting(query, sort);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ProjectTo<DocumentListItemDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return new PagedResult<DocumentListItemDto>
        {
            Items = items,
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<DocumentResponseDto?> GetDocumentByIdAsync(Guid id, Guid currentUserId, List<string> permissions,
        CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents
            .AsNoTracking()
            .Include(d => d.Category)
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (document == null)
            return null;

        if (!CanViewDocument(document, currentUserId, permissions))
            return null;

        return _mapper.Map<DocumentResponseDto>(document);
    }

    public async Task<DocumentResponseDto> CreateDocumentAsync(CreateDocumentDto createDto, Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (createDto.CategoryId.HasValue)
        {
            var categoryExists =
                await _context.Categories.AnyAsync(c => c.Id == createDto.CategoryId.Value, cancellationToken);
            if (!categoryExists)
                throw new InvalidOperationException($"Category with id {createDto.CategoryId} not found");
        }

        var document = _mapper.Map<Document>(createDto);
        document.Id = Guid.NewGuid();
        document.UserId = currentUserId;
        document.UploadDate = DateTime.UtcNow;

        _context.Documents.Add(document);

        var log = new ArchiveLog
        {
            Id = Guid.NewGuid(),
            Action = "Created",
            ActionType = ActionType.Created,
            IsCritical = false,
            Timestamp = DateTime.UtcNow,
            UserId = currentUserId,
            DocumentId = document.Id
        };
        _context.ArchiveLogs.Add(log);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Document {DocumentId} created by user {UserId}", document.Id, currentUserId);
        return _mapper.Map<DocumentResponseDto>(document);
    }

    public async Task UpdateDocumentAsync(Guid id, UpdateDocumentDto updateDto, Guid currentUserId,
        List<string> permissions, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (document == null)
            throw new KeyNotFoundException($"Document with id {id} not found");

        if (!CanEditDocument(document, currentUserId, permissions))
            throw new UnauthorizedAccessException("You do not have permission to edit this document");

        if (updateDto.CategoryId.HasValue)
        {
            var categoryExists =
                await _context.Categories.AnyAsync(c => c.Id == updateDto.CategoryId.Value, cancellationToken);
            if (!categoryExists)
                throw new InvalidOperationException($"Category with id {updateDto.CategoryId} not found");
        }

        _mapper.Map(updateDto, document);
        document.UpdatedAt = DateTime.UtcNow;

        var log = new ArchiveLog
        {
            Id = Guid.NewGuid(),
            Action = "Updated",
            ActionType = ActionType.Updated,
            IsCritical = false,
            Timestamp = DateTime.UtcNow,
            UserId = currentUserId,
            DocumentId = document.Id
        };
        _context.ArchiveLogs.Add(log);

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Document {DocumentId} updated by user {UserId}", id, currentUserId);
    }

    public async Task DeleteDocumentAsync(Guid id, Guid currentUserId, List<string> permissions,
        CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (document == null)
            throw new KeyNotFoundException($"Document with id {id} not found");

        if (!CanDeleteDocument(document, currentUserId, permissions))
            throw new UnauthorizedAccessException("You do not have permission to delete this document");

        // Добавляем проверку бизнес-правила
        if (!document.CanBeDeleted())
            throw new InvalidOperationException(
                "Document cannot be deleted because it was uploaded more than 30 days ago.");

        var log = new ArchiveLog
        {
            Id = Guid.NewGuid(),
            Action = "Deleted",
            ActionType = ActionType.Deleted,
            IsCritical = false,
            Timestamp = DateTime.UtcNow,
            UserId = currentUserId,
            DocumentId = document.Id
        };
        _context.ArchiveLogs.Add(log);

        _context.Documents.Remove(document);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Document {DocumentId} deleted by user {UserId}", id, currentUserId);
    }

    public async Task<PagedResult<ArchiveLogListItemDto>> GetDocumentLogsAsync(
        Guid documentId,
        int page,
        int pageSize,
        Guid currentUserId,
        List<string> permissions,
        CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);
        if (document == null)
            throw new KeyNotFoundException($"Document with id {documentId} not found");

        if (!CanViewDocument(document, currentUserId, permissions))
            throw new UnauthorizedAccessException("You do not have permission to view logs for this document");

        var query = _context.ArchiveLogs
            .AsNoTracking()
            .Where(l => l.DocumentId == documentId)
            .OrderByDescending(l => l.Timestamp);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ProjectTo<ArchiveLogListItemDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return new PagedResult<ArchiveLogListItemDto>
        {
            Items = items,
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<BulkOperationResult<Guid>> CreateBulkAsync(IEnumerable<CreateDocumentDto> createDtos,
        Guid currentUserId, List<string> permissions, CancellationToken cancellationToken = default)
    {
        var result = new BulkOperationResult<Guid>();
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var dto in createDtos)
                try
                {
                    var doc = await CreateDocumentInternalAsync(dto, currentUserId, cancellationToken);
                    result.Results.Add(new BulkOperationItem<Guid> { Id = doc.Id, Success = true });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Bulk create failed for document");
                    result.Results.Add(new BulkOperationItem<Guid> { Success = false, Error = ex.Message });
                }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return result;
    }

    public async Task<BulkOperationResult<Guid>> UpdateBulkAsync(IEnumerable<UpdateBulkDocumentDto> updateDtos,
        Guid currentUserId, List<string> permissions, CancellationToken cancellationToken = default)
    {
        var result = new BulkOperationResult<Guid>();
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var dto in updateDtos)
                try
                {
                    await UpdateDocumentInternalAsync(dto, currentUserId, permissions, cancellationToken);
                    result.Results.Add(new BulkOperationItem<Guid> { Id = dto.Id, Success = true });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Bulk update failed for document {DocumentId}", dto.Id);
                    result.Results.Add(new BulkOperationItem<Guid>
                        { Id = dto.Id, Success = false, Error = ex.Message });
                }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return result;
    }

    public async Task<BulkOperationResult<Guid>> DeleteBulkAsync(
        IEnumerable<Guid> ids,
        Guid currentUserId,
        List<string> permissions,
        CancellationToken cancellationToken = default)
    {
        var result = new BulkOperationResult<Guid>();
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var id in ids)
                try
                {
                    var document = await _context.Documents
                        .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
                    if (document == null)
                        throw new KeyNotFoundException($"Document with id {id} not found");

                    // Проверка прав на удаление
                    if (!CanDeleteDocument(document, currentUserId, permissions))
                        throw new UnauthorizedAccessException($"You do not have permission to delete document {id}");

                    // Проверка бизнес-правила: документ можно удалить только в течение 30 дней после загрузки
                    if (!document.CanBeDeleted())
                        throw new InvalidOperationException(
                            $"Document {id} cannot be deleted because it was uploaded more than 30 days ago.");

                    // Создание записи в логе архива
                    var log = new ArchiveLog
                    {
                        Id = Guid.NewGuid(),
                        Action = "Deleted",
                        ActionType = ActionType.Deleted,
                        IsCritical = false,
                        Timestamp = DateTime.UtcNow,
                        UserId = currentUserId,
                        DocumentId = document.Id
                    };
                    _context.ArchiveLogs.Add(log);

                    _context.Documents.Remove(document);
                    result.Results.Add(new BulkOperationItem<Guid> { Id = id, Success = true });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Bulk delete failed for document {DocumentId}", id);
                    result.Results.Add(new BulkOperationItem<Guid> { Id = id, Success = false, Error = ex.Message });
                }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return result;
    }

    public async Task<Dictionary<string, int>> GetDocumentsCountByCategoryAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Documents
            .Where(d => d.CategoryId != null)
            .GroupBy(d => d.Category!.Name)
            .Select(g => new { CategoryName = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryName, x => x.Count, cancellationToken);
    }

    public async Task<DocumentsStatisticsDto> GetDocumentsStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var totalCount = await _context.Documents.CountAsync(cancellationToken);
        var categoriesCount = await _context.Documents
            .Where(d => d.CategoryId != null)
            .GroupBy(d => d.Category!.Name)
            .Select(g => new CategoryCountDto { CategoryName = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var lastUploaded = await _context.Documents
            .OrderByDescending(d => d.UploadDate)
            .Select(d => new DocumentListItemDto
            {
                Id = d.Id,
                Title = d.Title,
                UploadDate = d.UploadDate
            })
            .FirstOrDefaultAsync(cancellationToken);

        return new DocumentsStatisticsDto
        {
            TotalDocuments = totalCount,
            DocumentsPerCategory = categoriesCount,
            LastUploadedDocument = lastUploaded
        };
    }

    private IQueryable<Document> ApplySorting(IQueryable<Document> query, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
            return query.OrderByDescending(d => d.UploadDate);

        var fields = sort.Split(',');
        IOrderedQueryable<Document>? orderedQuery = null;

        foreach (var field in fields)
        {
            var parts = field.Split(':');
            var fieldName = parts[0].Trim().ToLower();
            var direction = parts.Length > 1 ? parts[1].Trim().ToLower() : "asc";

            var isDescending = direction == "desc";

            if (orderedQuery == null)
                orderedQuery = fieldName switch
                {
                    "title" => isDescending ? query.OrderByDescending(d => d.Title) : query.OrderBy(d => d.Title),
                    "uploaddate" => isDescending
                        ? query.OrderByDescending(d => d.UploadDate)
                        : query.OrderBy(d => d.UploadDate),
                    _ => isDescending ? query.OrderByDescending(d => d.UploadDate) : query.OrderBy(d => d.UploadDate)
                };
            else
                orderedQuery = fieldName switch
                {
                    "title" => isDescending
                        ? orderedQuery.ThenByDescending(d => d.Title)
                        : orderedQuery.ThenBy(d => d.Title),
                    "uploaddate" => isDescending
                        ? orderedQuery.ThenByDescending(d => d.UploadDate)
                        : orderedQuery.ThenBy(d => d.UploadDate),
                    _ => isDescending
                        ? orderedQuery.ThenByDescending(d => d.UploadDate)
                        : orderedQuery.ThenBy(d => d.UploadDate)
                };
        }

        return orderedQuery ?? query.OrderByDescending(d => d.UploadDate);
    }

    private async Task<Document> CreateDocumentInternalAsync(CreateDocumentDto createDto, Guid currentUserId,
        CancellationToken cancellationToken)
    {
        if (createDto.CategoryId.HasValue)
        {
            var categoryExists =
                await _context.Categories.AnyAsync(c => c.Id == createDto.CategoryId.Value, cancellationToken);
            if (!categoryExists)
                throw new InvalidOperationException($"Category with id {createDto.CategoryId} not found");
        }

        var document = _mapper.Map<Document>(createDto);
        document.Id = Guid.NewGuid();
        document.UserId = currentUserId;
        document.UploadDate = DateTime.UtcNow;
        _context.Documents.Add(document);

        var log = new ArchiveLog
        {
            Id = Guid.NewGuid(),
            Action = "Created",
            ActionType = ActionType.Created,
            IsCritical = false,
            Timestamp = DateTime.UtcNow,
            UserId = currentUserId,
            DocumentId = document.Id
        };
        _context.ArchiveLogs.Add(log);

        return document;
    }

    private async Task UpdateDocumentInternalAsync(UpdateBulkDocumentDto dto, Guid currentUserId,
        List<string> permissions, CancellationToken cancellationToken)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == dto.Id, cancellationToken);
        if (document == null)
            throw new KeyNotFoundException($"Document with id {dto.Id} not found");

        if (!CanEditDocument(document, currentUserId, permissions))
            throw new UnauthorizedAccessException($"You do not have permission to edit document {dto.Id}");

        if (dto.CategoryId.HasValue)
        {
            var categoryExists =
                await _context.Categories.AnyAsync(c => c.Id == dto.CategoryId.Value, cancellationToken);
            if (!categoryExists)
                throw new InvalidOperationException($"Category with id {dto.CategoryId} not found");
        }

        _mapper.Map(dto, document);
        document.UpdatedAt = DateTime.UtcNow;

        var log = new ArchiveLog
        {
            Id = Guid.NewGuid(),
            Action = "Updated",
            ActionType = ActionType.Updated,
            IsCritical = false,
            Timestamp = DateTime.UtcNow,
            UserId = currentUserId,
            DocumentId = document.Id
        };
        _context.ArchiveLogs.Add(log);
    }

    #region Private helper methods for permission checks

    private bool CanViewDocument(Document document, Guid currentUserId, List<string> permissions)
    {
        return document.UserId == currentUserId || permissions.Contains("ViewAnyDocument");
    }

    private bool CanEditDocument(Document document, Guid currentUserId, List<string> permissions)
    {
        return (document.UserId == currentUserId && permissions.Contains("EditOwnDocuments"))
               || permissions.Contains("EditAnyDocument");
    }

    private bool CanDeleteDocument(Document document, Guid currentUserId, List<string> permissions)
    {
        return (document.UserId == currentUserId && permissions.Contains("DeleteOwnDocuments"))
               || permissions.Contains("DeleteAnyDocument");
    }

    #endregion
}