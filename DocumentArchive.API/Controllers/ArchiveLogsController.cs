using DocumentArchive.Core.DTOs.ArchiveLog;
using DocumentArchive.Core.DTOs.Shared;
using DocumentArchive.Core.DTOs.Statistics;
using DocumentArchive.Core.Interfaces.Services;
using DocumentArchive.Core.Models;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace DocumentArchive.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class ArchiveLogsController : ControllerBase
{
    private readonly IValidator<CreateArchiveLogDto> _createValidator;
    private readonly IArchiveLogService _logService;

    public ArchiveLogsController(
        IArchiveLogService logService,
        IValidator<CreateArchiveLogDto> createValidator)
    {
        _logService = logService;
        _createValidator = createValidator;
    }

    /// <summary>
    ///     Получает список логов с пагинацией и фильтрацией
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ArchiveLogListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResult<ArchiveLogListItemDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? documentId = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] ActionType? actionType = null,
        [FromQuery] bool? isCritical = null,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
            return BadRequest("Page must be >= 1.");
        if (pageSize < 1 || pageSize > 100)
            return BadRequest("Page size must be between 1 and 100.");
        if (fromDate.HasValue && toDate.HasValue && fromDate > toDate)
            return BadRequest("fromDate cannot be later than toDate.");

        var result = await _logService.GetLogsAsync(
            page, pageSize, documentId, userId, fromDate, toDate, actionType, isCritical, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    ///     Получает запись лога по ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ArchiveLogResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ArchiveLogResponseDto>> GetById(Guid id,
        CancellationToken cancellationToken = default)
    {
        var log = await _logService.GetLogByIdAsync(id, cancellationToken);
        if (log == null)
            return NotFound();
        return Ok(log);
    }

    /// <summary>
    ///     Создаёт запись в логе (вручную, для тестирования)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ArchiveLogResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ArchiveLogResponseDto>> Create([FromBody] CreateArchiveLogDto createDto,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _createValidator.ValidateAsync(createDto, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        var log = await _logService.CreateLogAsync(createDto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = log.Id }, log);
    }

    /// <summary>
    ///     Удаляет запись лога
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        await _logService.DeleteLogAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    ///     Получает количество логов по типам действий (статистика)
    /// </summary>
    [HttpGet("statistics/by-action-type")]
    [ProducesResponseType(typeof(Dictionary<ActionType, int>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Dictionary<ActionType, int>>> GetLogsCountByActionType(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _logService.GetLogsCountByActionTypeAsync(fromDate, toDate, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    ///     Получает общую статистику по логам
    /// </summary>
    [HttpGet("statistics/summary")]
    [ProducesResponseType(typeof(LogsStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LogsStatisticsDto>> GetLogsStatistics(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _logService.GetLogsStatisticsAsync(fromDate, toDate, cancellationToken);
        return Ok(result);
    }
}