using AutoMapper;
using DocumentArchive.Core.DTOs.ArchiveLog;
using DocumentArchive.Core.DTOs.Shared;
using DocumentArchive.Core.Interfaces.Repositorys;
using DocumentArchive.Core.Interfaces.Services;
using DocumentArchive.Core.Models;
using Microsoft.Extensions.Logging;

namespace DocumentArchive.Services.Services;

public class ArchiveLogService : IArchiveLogService
{
    private readonly IArchiveLogRepository _logRepo;
    private readonly IDocumentRepository _documentRepo;
    private readonly IUserRepository _userRepo;
    private readonly IMapper _mapper;
    private readonly ILogger<ArchiveLogService> _logger;

    public ArchiveLogService(
        IArchiveLogRepository logRepo,
        IDocumentRepository documentRepo,
        IUserRepository userRepo,
        IMapper mapper,
        ILogger<ArchiveLogService> logger)
    {
        _logRepo = logRepo;
        _documentRepo = documentRepo;
        _userRepo = userRepo;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResult<ArchiveLogListItemDto>> GetLogsAsync(
        int page,
        int pageSize,
        Guid? documentId,
        Guid? userId,
        DateTime? fromDate,
        DateTime? toDate,
        ActionType? actionType,
        bool? isCritical,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting logs page {Page} size {PageSize}", page, pageSize);
        var paged = await _logRepo.GetPagedAsync(page, pageSize, documentId, userId, fromDate, toDate, actionType, isCritical, cancellationToken);
        var dtos = _mapper.Map<List<ArchiveLogListItemDto>>(paged.Items);
        return new PagedResult<ArchiveLogListItemDto>
        {
            Items = dtos,
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount
        };
    }

    public async Task<ArchiveLogResponseDto?> GetLogByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var log = await _logRepo.GetByIdAsync(id, cancellationToken);
        return log == null ? null : _mapper.Map<ArchiveLogResponseDto>(log);
    }

    public async Task<ArchiveLogResponseDto> CreateLogAsync(CreateArchiveLogDto createDto, CancellationToken cancellationToken = default)
    {
        var document = await _documentRepo.GetByIdAsync(createDto.DocumentId, cancellationToken);
        if (document == null)
            throw new InvalidOperationException($"Document with id {createDto.DocumentId} not found");

        var user = await _userRepo.GetByIdAsync(createDto.UserId, cancellationToken);
        if (user == null)
            throw new InvalidOperationException($"User with id {createDto.UserId} not found");

        var log = _mapper.Map<ArchiveLog>(createDto);
        log.Id = Guid.NewGuid();
        log.Timestamp = DateTime.UtcNow;

        await _logRepo.AddAsync(log, cancellationToken);
        _logger.LogInformation("Log {LogId} created", log.Id);
        return _mapper.Map<ArchiveLogResponseDto>(log);
    }

    public async Task DeleteLogAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await _logRepo.GetByIdAsync(id, cancellationToken);
        if (existing == null)
            throw new KeyNotFoundException($"Log with id {id} not found");

        await _logRepo.DeleteAsync(id, cancellationToken);
        _logger.LogInformation("Log {LogId} deleted", id);
    }
}