using DocumentArchive.Core.DTOs.ArchiveLog;
using DocumentArchive.Core.DTOs.Document;
using DocumentArchive.Core.DTOs.Shared;
using DocumentArchive.Core.DTOs.Statistics;
using DocumentArchive.Core.Interfaces.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace DocumentArchive.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class DocumentsController : ControllerBase
{
    private const int MaxBulkSize = 100;
    private readonly IValidator<CreateDocumentDto> _createValidator;
    private readonly IDocumentService _documentService;
    private readonly ILogger<DocumentsController> _logger;
    private readonly IValidator<UpdateDocumentDto> _updateValidator;

    public DocumentsController(
        IDocumentService documentService,
        IValidator<CreateDocumentDto> createValidator,
        IValidator<UpdateDocumentDto> updateValidator,
        ILogger<DocumentsController> logger)
    {
        _documentService = documentService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _logger = logger;
    }

    /// <summary>
    ///     Получает список документов с пагинацией, фильтрацией, поиском и множественной сортировкой
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<DocumentListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResult<DocumentListItemDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] Guid[]? categoryIds = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? sort = null,
        CancellationToken cancellationToken = default)
    {
        // Валидация параметров
        if (page < 1)
            return BadRequest("Page must be greater than or equal to 1.");
        if (pageSize < 1 || pageSize > 100)
            return BadRequest("Page size must be between 1 and 100.");
        if (fromDate.HasValue && toDate.HasValue && fromDate > toDate)
            return BadRequest("fromDate cannot be later than toDate.");

        // Простая проверка формата сортировки
        if (!string.IsNullOrWhiteSpace(sort) && !IsValidSortFormat(sort))
            return BadRequest(
                "Invalid sort format. Expected format: field:direction,field:direction (e.g., title:asc,uploadDate:desc)");

        try
        {
            var result = await _documentService.GetDocumentsAsync(
                page, pageSize, search, categoryIds, userId, fromDate, toDate, sort, cancellationToken);
            return Ok(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request cancelled by client");
            return StatusCode(499, "Request cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting documents");
            return StatusCode(500,
                new { error = "An internal error occurred.", traceId = HttpContext.TraceIdentifier });
        }
    }

    /// <summary>
    ///     Получает документ по уникальному идентификатору
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(DocumentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DocumentResponseDto>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var document = await _documentService.GetDocumentByIdAsync(id, cancellationToken);
            if (document == null)
                return NotFound();

            return Ok(document);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request cancelled by client");
            return StatusCode(499, "Request cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document by ID {DocumentId}", id);
            return StatusCode(500,
                new { error = "An internal error occurred.", traceId = HttpContext.TraceIdentifier });
        }
    }

    /// <summary>
    ///     Создаёт новый документ
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(DocumentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DocumentResponseDto>> Create([FromBody] CreateDocumentDto createDto,
        CancellationToken cancellationToken = default)
    {
        // Ручная валидация
        var validationResult = await _createValidator.ValidateAsync(createDto, cancellationToken);
        if (!validationResult.IsValid) return BadRequest(validationResult.Errors);

        try
        {
            var result = await _documentService.CreateDocumentAsync(createDto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business rule violation in create document");
            return BadRequest(ex.Message);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request cancelled by client");
            return StatusCode(499, "Request cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating document");
            return StatusCode(500,
                new { error = "An internal error occurred.", traceId = HttpContext.TraceIdentifier });
        }
    }

    /// <summary>
    ///     Полностью обновляет документ
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDocumentDto updateDto,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _updateValidator.ValidateAsync(updateDto, cancellationToken);
        if (!validationResult.IsValid) return BadRequest(validationResult.Errors);

        try
        {
            await _documentService.UpdateDocumentAsync(id, updateDto, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business rule violation in update document");
            return BadRequest(ex.Message);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request cancelled by client");
            return StatusCode(499, "Request cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document {DocumentId}", id);
            return StatusCode(500,
                new { error = "An internal error occurred.", traceId = HttpContext.TraceIdentifier });
        }
    }

    /// <summary>
    ///     Удаляет документ
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _documentService.DeleteDocumentAsync(id, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request cancelled by client");
            return StatusCode(499, "Request cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {DocumentId}", id);
            return StatusCode(500,
                new { error = "An internal error occurred.", traceId = HttpContext.TraceIdentifier });
        }
    }

    /// <summary>
    ///     Получает историю операций с документом с пагинацией
    /// </summary>
    [HttpGet("{id}/logs")]
    [ProducesResponseType(typeof(PagedResult<ArchiveLogListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResult<ArchiveLogListItemDto>>> GetDocumentLogs(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
            return BadRequest("Page must be >= 1.");
        if (pageSize < 1 || pageSize > 100)
            return BadRequest("Page size must be between 1 and 100.");

        try
        {
            var result = await _documentService.GetDocumentLogsAsync(id, page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request cancelled by client");
            return StatusCode(499, "Request cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting logs for document {DocumentId}", id);
            return StatusCode(500,
                new { error = "An internal error occurred.", traceId = HttpContext.TraceIdentifier });
        }
    }

    /// <summary>
    ///     Массовое создание документов с детальным ответом
    /// </summary>
    [HttpPost("bulk")]
    [ProducesResponseType(typeof(BulkOperationResult<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BulkOperationResult<Guid>>> CreateBulk(
        [FromBody] List<CreateDocumentDto>? createDtos,
        CancellationToken cancellationToken = default)
    {
        if (createDtos == null || createDtos.Count == 0)
            return BadRequest("Bulk request cannot be empty.");
        if (createDtos.Count > MaxBulkSize)
            return BadRequest($"Too many items in bulk request. Maximum allowed: {MaxBulkSize}.");

        try
        {
            var result = await _documentService.CreateBulkAsync(createDtos, cancellationToken);
            return Ok(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request cancelled by client");
            return StatusCode(499, "Request cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk create");
            return StatusCode(500,
                new { error = "An internal error occurred.", traceId = HttpContext.TraceIdentifier });
        }
    }

    /// <summary>
    ///     Массовое обновление документов с детальным ответом
    /// </summary>
    [HttpPut("bulk")]
    [ProducesResponseType(typeof(BulkOperationResult<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BulkOperationResult<Guid>>> UpdateBulk(
        [FromBody] List<UpdateBulkDocumentDto>? updateDtos,
        CancellationToken cancellationToken = default)
    {
        if (updateDtos == null || updateDtos.Count == 0)
            return BadRequest("Bulk request cannot be empty.");
        if (updateDtos.Count > MaxBulkSize)
            return BadRequest($"Too many items in bulk request. Maximum allowed: {MaxBulkSize}.");

        try
        {
            var result = await _documentService.UpdateBulkAsync(updateDtos, cancellationToken);
            return Ok(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request cancelled by client");
            return StatusCode(499, "Request cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk update");
            return StatusCode(500,
                new { error = "An internal error occurred.", traceId = HttpContext.TraceIdentifier });
        }
    }

    /// <summary>
    ///     Массовое удаление документов с детальным ответом
    /// </summary>
    [HttpPost("bulk/delete")]
    [ProducesResponseType(typeof(BulkOperationResult<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BulkOperationResult<Guid>>> DeleteBulk(
        [FromBody] Guid[]? ids,
        CancellationToken cancellationToken = default)
    {
        if (ids == null || ids.Length == 0)
            return BadRequest("IDs cannot be empty.");
        if (ids.Length > MaxBulkSize)
            return BadRequest($"Too many items in bulk request. Maximum allowed: {MaxBulkSize}.");

        try
        {
            var result = await _documentService.DeleteBulkAsync(ids, cancellationToken);
            return Ok(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request cancelled by client");
            return StatusCode(499, "Request cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk delete");
            return StatusCode(500,
                new { error = "An internal error occurred.", traceId = HttpContext.TraceIdentifier });
        }
    }

    /// <summary>
    ///     Получает количество документов по категориям (статистика)
    /// </summary>
    [HttpGet("statistics/by-category")]
    [ProducesResponseType(typeof(Dictionary<string, int>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Dictionary<string, int>>> GetDocumentsCountByCategory(
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _documentService.GetDocumentsCountByCategoryAsync(cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document counts by category");
            return StatusCode(500,
                new { error = "An internal error occurred.", traceId = HttpContext.TraceIdentifier });
        }
    }

    /// <summary>
    ///     Получает общую статистику по документам
    /// </summary>
    [HttpGet("statistics/summary")]
    [ProducesResponseType(typeof(DocumentsStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DocumentsStatisticsDto>> GetDocumentsStatistics(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _documentService.GetDocumentsStatisticsAsync(cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document statistics");
            return StatusCode(500,
                new { error = "An internal error occurred.", traceId = HttpContext.TraceIdentifier });
        }
    }

    private static bool IsValidSortFormat(string sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
            return true;
        var parts = sort.Split(',');
        foreach (var part in parts)
        {
            var subParts = part.Split(':');
            if (subParts.Length > 2)
                return false;
            var field = subParts[0].Trim();
            if (string.IsNullOrEmpty(field))
                return false;
            if (subParts.Length == 2)
            {
                var dir = subParts[1].Trim().ToLowerInvariant();
                if (dir != "asc" && dir != "desc")
                    return false;
            }
        }

        return true;
    }
}