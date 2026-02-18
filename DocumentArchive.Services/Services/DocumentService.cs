using AutoMapper;
using AutoMapper.QueryableExtensions;
using DocumentArchive.Core.DTOs.ArchiveLog;
using DocumentArchive.Core.DTOs.Document;
using DocumentArchive.Core.DTOs.Shared;
using DocumentArchive.Core.Interfaces.Services;
using DocumentArchive.Core.Models;
using DocumentArchive.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocumentArchive.Services.Services;

public class DocumentService : IDocumentService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        AppDbContext context,
        IMapper mapper,
        ILogger<DocumentService> logger)
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
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting documents page {Page} size {PageSize}", page, pageSize);

        // Базовый запрос с AsNoTracking() для оптимизации чтения
        var query = _context.Documents
            .AsNoTracking()
            .AsQueryable();

        // Фильтрация
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(d => d.Title.Contains(search) ||
                                     (d.Description != null && d.Description.Contains(search)));

        if (categoryIds?.Any() == true)
            query = query.Where(d => d.CategoryId.HasValue && categoryIds.Contains(d.CategoryId.Value));

        if (userId.HasValue)
            query = query.Where(d => d.UserId == userId);

        if (fromDate.HasValue)
            query = query.Where(d => d.UploadDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(d => d.UploadDate <= toDate.Value);

        // Сортировка
        query = sort?.ToLower() switch
        {
            "title" => query.OrderBy(d => d.Title),
            "uploaddate" => query.OrderBy(d => d.UploadDate),
            "uploaddate_desc" => query.OrderByDescending(d => d.UploadDate),
            _ => query.OrderByDescending(d => d.UploadDate)
        };

        // Пагинация и проекция в DTO
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

    public async Task<DocumentResponseDto?> GetDocumentByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents
            .AsNoTracking()
            .Include(d => d.Category)
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        return document == null ? null : _mapper.Map<DocumentResponseDto>(document);
    }

    public async Task<DocumentResponseDto> CreateDocumentAsync(CreateDocumentDto createDto, CancellationToken cancellationToken = default)
    {
        // Проверка существования связанных сущностей
        if (createDto.CategoryId.HasValue)
        {
            var categoryExists = await _context.Categories
                .AnyAsync(c => c.Id == createDto.CategoryId.Value, cancellationToken);
            if (!categoryExists)
                throw new InvalidOperationException($"Category with id {createDto.CategoryId} not found");
        }

        if (createDto.UserId.HasValue)
        {
            var userExists = await _context.Users
                .AnyAsync(u => u.Id == createDto.UserId.Value, cancellationToken);
            if (!userExists)
                throw new InvalidOperationException($"User with id {createDto.UserId} not found");
        }

        var document = _mapper.Map<Document>(createDto);
        document.Id = Guid.NewGuid();
        document.UploadDate = DateTime.UtcNow;

        _context.Documents.Add(document);

        // Создаём запись в логе
        if (createDto.UserId.HasValue)
        {
            var log = new ArchiveLog
            {
                Id = Guid.NewGuid(),
                Action = "Created",
                ActionType = ActionType.Created,
                IsCritical = false,
                Timestamp = DateTime.UtcNow,
                UserId = createDto.UserId.Value,
                DocumentId = document.Id
            };
            _context.ArchiveLogs.Add(log);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Document {DocumentId} created", document.Id);
        return _mapper.Map<DocumentResponseDto>(document);
    }

    public async Task UpdateDocumentAsync(Guid id, UpdateDocumentDto updateDto, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (document == null)
            throw new KeyNotFoundException($"Document with id {id} not found");

        if (updateDto.CategoryId.HasValue)
        {
            var categoryExists = await _context.Categories
                .AnyAsync(c => c.Id == updateDto.CategoryId.Value, cancellationToken);
            if (!categoryExists)
                throw new InvalidOperationException($"Category with id {updateDto.CategoryId} not found");
        }

        _mapper.Map(updateDto, document);
        document.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Document {DocumentId} updated", id);
    }

    public async Task DeleteDocumentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (document == null)
            throw new KeyNotFoundException($"Document with id {id} not found");

        _context.Documents.Remove(document);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Document {DocumentId} deleted", id);
    }

    public async Task<PagedResult<ArchiveLogListItemDto>> GetDocumentLogsAsync(
        Guid documentId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var documentExists = await _context.Documents
            .AnyAsync(d => d.Id == documentId, cancellationToken);
        if (!documentExists)
            throw new KeyNotFoundException($"Document with id {documentId} not found");

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

    public async Task<BulkOperationResult<Guid>> CreateBulkAsync(
        IEnumerable<CreateDocumentDto> createDtos,
        CancellationToken cancellationToken = default)
    {
        var result = new BulkOperationResult<Guid>();
        // Используем транзакцию для атомарности всех операций
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var dto in createDtos)
            {
                try
                {
                    var doc = await CreateDocumentInternalAsync(dto, cancellationToken);
                    result.Results.Add(new BulkOperationItem<Guid> { Id = doc.Id, Success = true });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Bulk create failed for document");
                    result.Results.Add(new BulkOperationItem<Guid> { Success = false, Error = ex.Message });
                }
            }
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        return result;
    }

    // Внутренний метод без SaveChanges, чтобы можно было группировать в транзакции
    private async Task<Document> CreateDocumentInternalAsync(CreateDocumentDto createDto, CancellationToken cancellationToken)
    {
        // Проверки (можно вынести в отдельный приватный метод)
        if (createDto.CategoryId.HasValue)
        {
            var categoryExists = await _context.Categories
                .AnyAsync(c => c.Id == createDto.CategoryId.Value, cancellationToken);
            if (!categoryExists)
                throw new InvalidOperationException($"Category with id {createDto.CategoryId} not found");
        }
        if (createDto.UserId.HasValue)
        {
            var userExists = await _context.Users
                .AnyAsync(u => u.Id == createDto.UserId.Value, cancellationToken);
            if (!userExists)
                throw new InvalidOperationException($"User with id {createDto.UserId} not found");
        }

        var document = _mapper.Map<Document>(createDto);
        document.Id = Guid.NewGuid();
        document.UploadDate = DateTime.UtcNow;
        _context.Documents.Add(document);

        if (createDto.UserId.HasValue)
        {
            var log = new ArchiveLog
            {
                Id = Guid.NewGuid(),
                Action = "Created",
                ActionType = ActionType.Created,
                IsCritical = false,
                Timestamp = DateTime.UtcNow,
                UserId = createDto.UserId.Value,
                DocumentId = document.Id
            };
            _context.ArchiveLogs.Add(log);
        }

        // НЕ вызываем SaveChangesAsync — это сделает внешний код после коммита
        return document;
    }

    // Аналогично UpdateBulkAsync и DeleteBulkAsync (опущено для краткости)
    public async Task<BulkOperationResult<Guid>> UpdateBulkAsync(
        IEnumerable<UpdateBulkDocumentDto> updateDtos,
        CancellationToken cancellationToken = default)
    {
        var result = new BulkOperationResult<Guid>();
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var dto in updateDtos)
            {
                try
                {
                    await UpdateDocumentInternalAsync(dto, cancellationToken);
                    result.Results.Add(new BulkOperationItem<Guid> { Id = dto.Id, Success = true });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Bulk update failed for document {DocumentId}", dto.Id);
                    result.Results.Add(new BulkOperationItem<Guid> { Id = dto.Id, Success = false, Error = ex.Message });
                }
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

    private async Task UpdateDocumentInternalAsync(UpdateBulkDocumentDto dto, CancellationToken cancellationToken)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == dto.Id, cancellationToken);
        if (document == null)
            throw new KeyNotFoundException($"Document with id {dto.Id} not found");

        if (dto.CategoryId.HasValue)
        {
            var categoryExists = await _context.Categories
                .AnyAsync(c => c.Id == dto.CategoryId.Value, cancellationToken);
            if (!categoryExists)
                throw new InvalidOperationException($"Category with id {dto.CategoryId} not found");
        }

        _mapper.Map(dto, document);
        document.UpdatedAt = DateTime.UtcNow;
        // Контекст отслеживает изменения, SaveChangesAsync вызовем позже
    }

    public async Task<BulkOperationResult<Guid>> DeleteBulkAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        var result = new BulkOperationResult<Guid>();
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var id in ids)
            {
                try
                {
                    var document = await _context.Documents
                        .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
                    if (document == null)
                        throw new KeyNotFoundException($"Document with id {id} not found");

                    _context.Documents.Remove(document);
                    result.Results.Add(new BulkOperationItem<Guid> { Id = id, Success = true });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Bulk delete failed for document {DocumentId}", id);
                    result.Results.Add(new BulkOperationItem<Guid> { Id = id, Success = false, Error = ex.Message });
                }
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
}