using AutoMapper;
using DocumentArchive.Core.DTOs.ArchiveLog;
using DocumentArchive.Core.DTOs.Shared;
using DocumentArchive.Core.Models;
using DocumentArchive.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace DocumentArchive.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class ArchiveLogsController : ControllerBase
{
    private readonly ArchiveLogRepository _logRepo;
    private readonly DocumentRepository _documentRepo;
    private readonly UserRepository _userRepo;
    private readonly IMapper _mapper;

    public ArchiveLogsController(
        ArchiveLogRepository logRepo,
        DocumentRepository documentRepo,
        UserRepository userRepo,
        IMapper mapper)
    {
        _logRepo = logRepo;
        _documentRepo = documentRepo;
        _userRepo = userRepo;
        _mapper = mapper;
    }

    /// <summary>
    /// Получает список логов с пагинацией и фильтрацией
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ArchiveLogListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ArchiveLogListItemDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? documentId = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] ActionType? actionType = null,
        [FromQuery] bool? isCritical = null)
    {
        var logs = await _logRepo.GetAllAsync();

        if (documentId.HasValue)
            logs = logs.Where(l => l.DocumentId == documentId);
        if (userId.HasValue)
            logs = logs.Where(l => l.UserId == userId);
        if (fromDate.HasValue)
            logs = logs.Where(l => l.Timestamp >= fromDate.Value);
        if (toDate.HasValue)
            logs = logs.Where(l => l.Timestamp <= toDate.Value);
        if (actionType.HasValue)
            logs = logs.Where(l => l.ActionType == actionType.Value);
        if (isCritical.HasValue)
            logs = logs.Where(l => l.IsCritical == isCritical.Value);

        logs = logs.OrderByDescending(l => l.Timestamp);

        var totalCount = logs.Count();
        var items = logs
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var dtos = _mapper.Map<List<ArchiveLogListItemDto>>(items);
        var result = new PagedResult<ArchiveLogListItemDto>
        {
            Items = dtos,
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return Ok(result);
    }

    /// <summary>
    /// Получает запись лога по ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ArchiveLogResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ArchiveLogResponseDto>> GetById(Guid id)
    {
        var log = await _logRepo.GetByIdAsync(id);
        if (log == null)
            return NotFound();

        var dto = _mapper.Map<ArchiveLogResponseDto>(log);
        return Ok(dto);
    }

    /// <summary>
    /// Создаёт запись в логе (вручную, для тестирования)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ArchiveLogResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ArchiveLogResponseDto>> Create([FromBody] CreateArchiveLogDto createDto)
    {
        var document = await _documentRepo.GetByIdAsync(createDto.DocumentId);
        if (document == null)
            return BadRequest($"Document with id {createDto.DocumentId} not found");

        var user = await _userRepo.GetByIdAsync(createDto.UserId);
        if (user == null)
            return BadRequest($"User with id {createDto.UserId} not found");

        var log = _mapper.Map<ArchiveLog>(createDto);
        log.Id = Guid.NewGuid();
        log.Timestamp = DateTime.UtcNow;

        await _logRepo.AddAsync(log);

        var dto = _mapper.Map<ArchiveLogResponseDto>(log);
        return CreatedAtAction(nameof(GetById), new { id = log.Id }, dto);
    }

    /// <summary>
    /// Удаляет запись лога
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var existing = await _logRepo.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        await _logRepo.DeleteAsync(id);
        return NoContent();
    }
}