using AutoMapper;
using AutoMapper.QueryableExtensions;
using DocumentArchive.Core.DTOs.ArchiveLog;
using DocumentArchive.Core.DTOs.Shared;
using DocumentArchive.Core.DTOs.Statistics;
using DocumentArchive.Core.Interfaces.Services;
using DocumentArchive.Core.Models;
using DocumentArchive.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocumentArchive.Services.Services;

public class ArchiveLogService : IArchiveLogService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<ArchiveLogService> _logger;

    public ArchiveLogService(
        AppDbContext context,
        IMapper mapper,
        ILogger<ArchiveLogService> logger)
    {
        _context = context;
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

        var query = _context.ArchiveLogs
            .AsNoTracking()
            .AsQueryable();

        if (documentId.HasValue)
            query = query.Where(l => l.DocumentId == documentId);
        if (userId.HasValue)
            query = query.Where(l => l.UserId == userId);
        if (fromDate.HasValue)
            query = query.Where(l => l.Timestamp >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(l => l.Timestamp <= toDate.Value);
        if (actionType.HasValue)
            query = query.Where(l => l.ActionType == actionType.Value);
        if (isCritical.HasValue)
            query = query.Where(l => l.IsCritical == isCritical.Value);

        // Сортировка по убыванию времени
        query = query.OrderByDescending(l => l.Timestamp);

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

    public async Task<ArchiveLogResponseDto?> GetLogByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var log = await _context.ArchiveLogs
            .AsNoTracking()
            .Include(l => l.User)
            .Include(l => l.Document)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

        return log == null ? null : _mapper.Map<ArchiveLogResponseDto>(log);
    }

    public async Task<ArchiveLogResponseDto> CreateLogAsync(CreateArchiveLogDto createDto, CancellationToken cancellationToken = default)
    {
        // Проверка существования связанных сущностей
        var documentExists = await _context.Documents
            .AnyAsync(d => d.Id == createDto.DocumentId, cancellationToken);
        if (!documentExists)
            throw new InvalidOperationException($"Document with id {createDto.DocumentId} not found");

        var userExists = await _context.Users
            .AnyAsync(u => u.Id == createDto.UserId, cancellationToken);
        if (!userExists)
            throw new InvalidOperationException($"User with id {createDto.UserId} not found");

        var log = _mapper.Map<ArchiveLog>(createDto);
        log.Id = Guid.NewGuid();
        log.Timestamp = DateTime.UtcNow;

        _context.ArchiveLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Log {LogId} created", log.Id);
        return _mapper.Map<ArchiveLogResponseDto>(log);
    }

    public async Task DeleteLogAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var log = await _context.ArchiveLogs
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (log == null)
            throw new KeyNotFoundException($"Log with id {id} not found");

        _context.ArchiveLogs.Remove(log);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Log {LogId} deleted", id);
    }
    
    public async Task<Dictionary<ActionType, int>> GetLogsCountByActionTypeAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var query = _context.ArchiveLogs.AsNoTracking();

        if (fromDate.HasValue)
            query = query.Where(l => l.Timestamp >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(l => l.Timestamp <= toDate.Value);

        return await query
            .GroupBy(l => l.ActionType)
            .Select(g => new { ActionType = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ActionType, x => x.Count, cancellationToken);
    }

    public async Task<LogsStatisticsDto> GetLogsStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var query = _context.ArchiveLogs.AsNoTracking();

        if (fromDate.HasValue)
            query = query.Where(l => l.Timestamp >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(l => l.Timestamp <= toDate.Value);

        var totalLogs = await query.CountAsync(cancellationToken);
        var criticalLogs = await query.CountAsync(l => l.IsCritical, cancellationToken);
        var logsByType = await query
            .GroupBy(l => l.ActionType)
            .Select(g => new ActionTypeCountDto { ActionType = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return new LogsStatisticsDto
        {
            TotalLogs = totalLogs,
            CriticalLogs = criticalLogs,
            LogsByActionType = logsByType
        };
    }
}