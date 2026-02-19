using DocumentArchive.Core.DTOs.DocumentVersion;
using DocumentArchive.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace DocumentArchive.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class DocumentVersionsController : ControllerBase
{
    private readonly IDocumentVersionService _documentVersionService;
    private readonly ILogger<DocumentVersionsController> _logger;

    public DocumentVersionsController(IDocumentVersionService documentVersionService,
        ILogger<DocumentVersionsController> logger)
    {
        _documentVersionService = documentVersionService;
        _logger = logger;
    }

    /// <summary>
    ///     Получает список версий (можно фильтровать по documentId)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<DocumentVersionListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<DocumentVersionListItemDto>>> GetAll(
        [FromQuery] Guid? documentId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var versions = await _documentVersionService.GetAllAsync(documentId, cancellationToken);
            return Ok(versions);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request cancelled by client");
            return StatusCode(499, "Request cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document versions");
            return StatusCode(500,
                new { error = "An internal error occurred.", traceId = HttpContext.TraceIdentifier });
        }
    }

    /// <summary>
    ///     Получает версию по ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(DocumentVersionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DocumentVersionResponseDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var version = await _documentVersionService.GetByIdAsync(id, cancellationToken);
            if (version == null)
                return NotFound();
            return Ok(version);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request cancelled by client");
            return StatusCode(499, "Request cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document version by ID {VersionId}", id);
            return StatusCode(500,
                new { error = "An internal error occurred.", traceId = HttpContext.TraceIdentifier });
        }
    }

    /// <summary>
    ///     Создаёт новую версию документа
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(DocumentVersionResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DocumentVersionResponseDto>> Create([FromBody] CreateDocumentVersionDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var version = await _documentVersionService.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = version.Id }, version);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business rule violation in create document version");
            return BadRequest(ex.Message);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request cancelled by client");
            return StatusCode(499, "Request cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating document version");
            return StatusCode(500,
                new { error = "An internal error occurred.", traceId = HttpContext.TraceIdentifier });
        }
    }

    /// <summary>
    ///     Обновляет комментарий версии (остальные поля неизменяемы)
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDocumentVersionDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            await _documentVersionService.UpdateAsync(id, dto, cancellationToken);
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
            _logger.LogError(ex, "Error updating document version {VersionId}", id);
            return StatusCode(500,
                new { error = "An internal error occurred.", traceId = HttpContext.TraceIdentifier });
        }
    }

    /// <summary>
    ///     Удаляет версию документа
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _documentVersionService.DeleteAsync(id, cancellationToken);
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
            _logger.LogError(ex, "Error deleting document version {VersionId}", id);
            return StatusCode(500,
                new { error = "An internal error occurred.", traceId = HttpContext.TraceIdentifier });
        }
    }
}