using AutoMapper;
using DocumentArchive.Core.DTOs.ArchiveLog;
using DocumentArchive.Core.DTOs.Document;
using DocumentArchive.Core.DTOs.Shared;
using DocumentArchive.Core.Interfaces.Repositorys;
using DocumentArchive.Core.Interfaces.Services;
using DocumentArchive.Core.Models;
using Microsoft.Extensions.Logging;

namespace DocumentArchive.Services.Services;

public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _documentRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly IUserRepository _userRepo;
    private readonly IArchiveLogRepository _logRepo;
    private readonly IMapper _mapper;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        IDocumentRepository documentRepo,
        ICategoryRepository categoryRepo,
        IUserRepository userRepo,
        IArchiveLogRepository logRepo,
        IMapper mapper,
        ILogger<DocumentService> logger)
    {
        _documentRepo = documentRepo;
        _categoryRepo = categoryRepo;
        _userRepo = userRepo;
        _logRepo = logRepo;
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
        _logger.LogInformation("Getting documents page {Page} size {PageSize}, search '{Search}', categoryIds {CategoryIds}, userId {UserId}, fromDate {FromDate}, toDate {ToDate}, sort {Sort}",
            page, pageSize, search, categoryIds, userId, fromDate, toDate, sort);

        var paged = await _documentRepo.GetPagedAsync(
            page, pageSize, search, categoryIds, userId, fromDate, toDate, sort, cancellationToken);

        var dtos = _mapper.Map<List<DocumentListItemDto>>(paged.Items);
        return new PagedResult<DocumentListItemDto>
        {
            Items = dtos,
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount
        };
    }

    public async Task<DocumentResponseDto?> GetDocumentByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var document = await _documentRepo.GetByIdAsync(id, cancellationToken);
        return document == null ? null : _mapper.Map<DocumentResponseDto>(document);
    }

    public async Task<DocumentResponseDto> CreateDocumentAsync(CreateDocumentDto createDto, CancellationToken cancellationToken = default)
    {
        // Проверка существования связанных сущностей
        if (createDto.CategoryId.HasValue)
        {
            var category = await _categoryRepo.GetByIdAsync(createDto.CategoryId.Value, cancellationToken);
            if (category == null)
                throw new InvalidOperationException($"Category with id {createDto.CategoryId} not found");
        }

        if (createDto.UserId.HasValue)
        {
            var user = await _userRepo.GetByIdAsync(createDto.UserId.Value, cancellationToken);
            if (user == null)
                throw new InvalidOperationException($"User with id {createDto.UserId} not found");
        }

        var document = _mapper.Map<Document>(createDto);
        document.Id = Guid.NewGuid();
        document.UploadDate = DateTime.UtcNow;

        await _documentRepo.AddAsync(document, cancellationToken);

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
            await _logRepo.AddAsync(log, cancellationToken);
        }

        _logger.LogInformation("Document {DocumentId} created", document.Id);
        return _mapper.Map<DocumentResponseDto>(document);
    }

    public async Task UpdateDocumentAsync(Guid id, UpdateDocumentDto updateDto, CancellationToken cancellationToken = default)
    {
        var existing = await _documentRepo.GetByIdAsync(id, cancellationToken);
        if (existing == null)
            throw new KeyNotFoundException($"Document with id {id} not found");

        if (updateDto.CategoryId.HasValue)
        {
            var category = await _categoryRepo.GetByIdAsync(updateDto.CategoryId.Value, cancellationToken);
            if (category == null)
                throw new InvalidOperationException($"Category with id {updateDto.CategoryId} not found");
        }

        _mapper.Map(updateDto, existing);
        existing.UpdatedAt = DateTime.UtcNow;
        await _documentRepo.UpdateAsync(existing, cancellationToken);
        _logger.LogInformation("Document {DocumentId} updated", id);
    }

    public async Task DeleteDocumentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await _documentRepo.GetByIdAsync(id, cancellationToken);
        if (existing == null)
            throw new KeyNotFoundException($"Document with id {id} not found");

        await _documentRepo.DeleteAsync(id, cancellationToken);
        _logger.LogInformation("Document {DocumentId} deleted", id);
    }

    public async Task<PagedResult<ArchiveLogListItemDto>> GetDocumentLogsAsync(
        Guid documentId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var document = await _documentRepo.GetByIdAsync(documentId, cancellationToken);
        if (document == null)
            throw new KeyNotFoundException($"Document with id {documentId} not found");

        var logs = await _logRepo.GetByDocumentIdAsync(documentId, cancellationToken);
        var archiveLogs = logs as ArchiveLog[] ?? logs.ToArray();
        var totalCount = archiveLogs.Count();
        var items = archiveLogs
            .OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var dtos = _mapper.Map<List<ArchiveLogListItemDto>>(items);
        return new PagedResult<ArchiveLogListItemDto>
        {
            Items = dtos,
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
        foreach (var dto in createDtos)
        {
            try
            {
                var doc = await CreateDocumentAsync(dto, cancellationToken);
                result.Results.Add(new BulkOperationItem<Guid> { Id = doc.Id, Success = true });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Bulk create failed for document");
                result.Results.Add(new BulkOperationItem<Guid> { Success = false, Error = "Operation failed" });
            }
        }
        return result;
    }

    public async Task<BulkOperationResult<Guid>> UpdateBulkAsync(
        IEnumerable<UpdateBulkDocumentDto> updateDtos,
        CancellationToken cancellationToken = default)
    {
        var result = new BulkOperationResult<Guid>();
        foreach (var dto in updateDtos)
        {
            try
            {
                await UpdateDocumentAsync(dto.Id, new UpdateDocumentDto
                {
                    Title = dto.Title,
                    Description = dto.Description,
                    FileName = dto.FileName,
                    CategoryId = dto.CategoryId
                }, cancellationToken);
                result.Results.Add(new BulkOperationItem<Guid> { Id = dto.Id, Success = true });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Bulk update failed for document {DocumentId}", dto.Id);
                result.Results.Add(new BulkOperationItem<Guid> { Id = dto.Id, Success = false, Error = "Operation failed" });
            }
        }
        return result;
    }

    public async Task<BulkOperationResult<Guid>> DeleteBulkAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        var result = new BulkOperationResult<Guid>();
        foreach (var id in ids)
        {
            try
            {
                await DeleteDocumentAsync(id, cancellationToken);
                result.Results.Add(new BulkOperationItem<Guid> { Id = id, Success = true });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Bulk delete failed for document {DocumentId}", id);
                result.Results.Add(new BulkOperationItem<Guid> { Id = id, Success = false, Error = "Operation failed" });
            }
        }
        return result;
    }
}