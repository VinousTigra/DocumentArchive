using DocumentArchive.Core.DTOs.Shared;
using DocumentArchive.Core.Models;
using DocumentArchive.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocumentArchive.API.Controllers;

[Authorize(Roles = "Admin")]
[Route("api/admin/audit")]
[ApiController]
public class AuditController : ControllerBase
{
    private readonly AppDbContext _context;

    public AuditController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    ///     Получает список логов безопасности с пагинацией и фильтрацией
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<SecurityAuditLog>>> GetAuditLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] SecurityEventType? eventType = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var query = _context.SecurityAuditLogs.AsNoTracking();

        if (eventType.HasValue)
            query = query.Where(l => l.EventType == eventType.Value);
        if (userId.HasValue)
            query = query.Where(l => l.UserId == userId.Value);
        if (fromDate.HasValue)
            query = query.Where(l => l.Timestamp >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(l => l.Timestamp <= toDate.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new PagedResult<SecurityAuditLog>
        {
            Items = items,
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }

    /// <summary>
    ///     Получает логи конкретного пользователя
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<List<SecurityAuditLog>>> GetUserLogs(Guid userId)
    {
        var logs = await _context.SecurityAuditLogs
            .AsNoTracking()
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.Timestamp)
            .ToListAsync();
        return Ok(logs);
    }
}