using AutoMapper;
using DocumentArchive.Core.DTOs.DocumentVersion;
using DocumentArchive.Core.Models;
using DocumentArchive.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocumentArchive.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class DocumentVersionsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<DocumentVersionsController> _logger;
    private readonly IMapper _mapper;

    public DocumentVersionsController(AppDbContext context, IMapper mapper, ILogger<DocumentVersionsController> logger)
    {
        _context = context;
        _mapper = mapper;
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
            var query = _context.DocumentVersions
                .AsNoTracking()
                .AsQueryable();

            if (documentId.HasValue)
                query = query.Where(v => v.DocumentId == documentId.Value);

            var versions = await query
                .OrderByDescending(v => v.UploadedAt)
                .ToListAsync(cancellationToken);
            return Ok(_mapper.Map<List<DocumentVersionListItemDto>>(versions));
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
            var version = await _context.DocumentVersions
                .AsNoTracking()
                .Include(v => v.Document)
                .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
            if (version == null)
                return NotFound();
            return Ok(_mapper.Map<DocumentVersionResponseDto>(version));
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
            var documentExists = await _context.Documents.AnyAsync(d => d.Id == dto.DocumentId, cancellationToken);
            if (!documentExists)
                return BadRequest($"Document with id {dto.DocumentId} not found.");

            var versionExists = await _context.DocumentVersions
                .AnyAsync(v => v.DocumentId == dto.DocumentId && v.VersionNumber == dto.VersionNumber,
                    cancellationToken);
            if (versionExists)
                return BadRequest($"Version number {dto.VersionNumber} already exists for this document.");

            var version = _mapper.Map<DocumentVersion>(dto);
            version.Id = Guid.NewGuid();
            version.UploadedAt = DateTime.UtcNow;

            _context.DocumentVersions.Add(version);
            await _context.SaveChangesAsync(cancellationToken);

            var response = _mapper.Map<DocumentVersionResponseDto>(version);
            return CreatedAtAction(nameof(GetById), new { id = version.Id }, response);
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
            var version = await _context.DocumentVersions.FindAsync(new object[] { id }, cancellationToken);
            if (version == null)
                return NotFound();

            _mapper.Map(dto, version);
            await _context.SaveChangesAsync(cancellationToken);
            return NoContent();
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
            var version = await _context.DocumentVersions.FindAsync(new object[] { id }, cancellationToken);
            if (version == null)
                return NotFound();

            _context.DocumentVersions.Remove(version);
            await _context.SaveChangesAsync(cancellationToken);
            return NoContent();
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