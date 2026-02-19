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

    public DocumentVersionsController(IDocumentVersionService documentVersionService)
    {
        _documentVersionService = documentVersionService;
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
        var versions = await _documentVersionService.GetAllAsync(documentId, cancellationToken);
        return Ok(versions);
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
        var version = await _documentVersionService.GetByIdAsync(id, cancellationToken);
        if (version == null)
            return NotFound();
        return Ok(version);
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
        var version = await _documentVersionService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = version.Id }, version);
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
        await _documentVersionService.UpdateAsync(id, dto, cancellationToken);
        return NoContent();
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
        await _documentVersionService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}