using DocumentArchive.Core.DTOs.Permission;
using DocumentArchive.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace DocumentArchive.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class PermissionsController : ControllerBase
{
    private readonly IPermissionService _permissionService;

    public PermissionsController(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    /// <summary>
    ///     Получает список всех прав
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<PermissionListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<PermissionListItemDto>>> GetAll(CancellationToken cancellationToken)
    {
        var permissions = await _permissionService.GetAllAsync(cancellationToken);
        return Ok(permissions);
    }

    /// <summary>
    ///     Получает право по ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(PermissionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PermissionResponseDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var permission = await _permissionService.GetByIdAsync(id, cancellationToken);
        if (permission == null)
            return NotFound();
        return Ok(permission);
    }

    /// <summary>
    ///     Создаёт новое право
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PermissionResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PermissionResponseDto>> Create([FromBody] CreatePermissionDto dto,
        CancellationToken cancellationToken)
    {
        var permission = await _permissionService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = permission.Id }, permission);
    }

    /// <summary>
    ///     Обновляет право
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePermissionDto dto,
        CancellationToken cancellationToken)
    {
        await _permissionService.UpdateAsync(id, dto, cancellationToken);
        return NoContent();
    }

    /// <summary>
    ///     Удаляет право
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _permissionService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}