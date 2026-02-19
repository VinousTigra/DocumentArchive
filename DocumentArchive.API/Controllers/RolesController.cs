using DocumentArchive.Core.DTOs.Role;
using DocumentArchive.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace DocumentArchive.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    /// <summary>
    ///     Получает список всех ролей
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<RoleListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<RoleListItemDto>>> GetAll(CancellationToken cancellationToken)
    {
        var roles = await _roleService.GetAllAsync(cancellationToken);
        return Ok(roles);
    }

    /// <summary>
    ///     Получает роль по ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(RoleResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RoleResponseDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var role = await _roleService.GetByIdAsync(id, cancellationToken);
        if (role == null)
            return NotFound();
        return Ok(role);
    }

    /// <summary>
    ///     Создаёт новую роль
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(RoleResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RoleResponseDto>> Create([FromBody] CreateRoleDto dto,
        CancellationToken cancellationToken)
    {
        var role = await _roleService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = role.Id }, role);
    }

    /// <summary>
    ///     Обновляет роль
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoleDto dto, CancellationToken cancellationToken)
    {
        await _roleService.UpdateAsync(id, dto, cancellationToken);
        return NoContent();
    }

    /// <summary>
    ///     Удаляет роль
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _roleService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    ///     Назначает право роли
    /// </summary>
    [HttpPost("{roleId}/permissions/{permissionId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AssignPermission(Guid roleId, Guid permissionId,
        CancellationToken cancellationToken)
    {
        await _roleService.AssignPermissionAsync(roleId, permissionId, cancellationToken);
        return Ok();
    }

    /// <summary>
    ///     Удаляет право у роли
    /// </summary>
    [HttpDelete("{roleId}/permissions/{permissionId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RemovePermission(Guid roleId, Guid permissionId,
        CancellationToken cancellationToken)
    {
        await _roleService.RemovePermissionAsync(roleId, permissionId, cancellationToken);
        return NoContent();
    }
}