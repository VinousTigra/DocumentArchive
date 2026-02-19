using AutoMapper;
using DocumentArchive.Core.DTOs.Role;
using DocumentArchive.Core.Models;
using DocumentArchive.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocumentArchive.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class RolesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<RolesController> _logger;
    private readonly IMapper _mapper;

    public RolesController(AppDbContext context, IMapper mapper, ILogger<RolesController> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    ///     Получает список всех ролей
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<RoleListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<RoleListItemDto>>> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var roles = await _context.Roles
                .AsNoTracking()
                .OrderBy(r => r.Name)
                .ToListAsync(cancellationToken);
            return Ok(_mapper.Map<List<RoleListItemDto>>(roles));
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request cancelled by client");
            return StatusCode(499, "Request cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting roles");
            return StatusCode(500,
                new { error = "An internal error occurred.", traceId = HttpContext.TraceIdentifier });
        }
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
        try
        {
            var role = await _context.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
            if (role == null)
                return NotFound();
            return Ok(_mapper.Map<RoleResponseDto>(role));
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request cancelled by client");
            return StatusCode(499, "Request cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting role by ID {RoleId}", id);
            return StatusCode(500,
                new { error = "An internal error occurred.", traceId = HttpContext.TraceIdentifier });
        }
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
        try
        {
            var exists = await _context.Roles.AnyAsync(r => r.Name == dto.Name, cancellationToken);
            if (exists)
                return BadRequest($"Role with name '{dto.Name}' already exists.");

            var role = _mapper.Map<Role>(dto);
            role.Id = Guid.NewGuid();
            role.CreatedAt = DateTime.UtcNow;

            _context.Roles.Add(role);
            await _context.SaveChangesAsync(cancellationToken);

            var response = _mapper.Map<RoleResponseDto>(role);
            return CreatedAtAction(nameof(GetById), new { id = role.Id }, response);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request cancelled by client");
            return StatusCode(499, "Request cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role");
            return StatusCode(500,
                new { error = "An internal error occurred.", traceId = HttpContext.TraceIdentifier });
        }
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
        try
        {
            var role = await _context.Roles.FindAsync(new object[] { id }, cancellationToken);
            if (role == null)
                return NotFound();

            if (!string.IsNullOrWhiteSpace(dto.Name) && dto.Name != role.Name)
            {
                var exists = await _context.Roles.AnyAsync(r => r.Name == dto.Name && r.Id != id, cancellationToken);
                if (exists)
                    return BadRequest($"Role with name '{dto.Name}' already exists.");
            }

            _mapper.Map(dto, role);
            role.UpdatedAt = DateTime.UtcNow;

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
            _logger.LogError(ex, "Error updating role {RoleId}", id);
            return StatusCode(500,
                new { error = "An internal error occurred.", traceId = HttpContext.TraceIdentifier });
        }
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
        try
        {
            var role = await _context.Roles
                .Include(r => r.UserRoles)
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
            if (role == null)
                return NotFound();

            if (role.UserRoles.Any() || role.RolePermissions.Any())
                return BadRequest("Cannot delete role because it has assigned users or permissions.");

            _context.Roles.Remove(role);
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
            _logger.LogError(ex, "Error deleting role {RoleId}", id);
            return StatusCode(500,
                new { error = "An internal error occurred.", traceId = HttpContext.TraceIdentifier });
        }
    }

    /// <summary>
    ///     Назначает право роли
    /// </summary>
    [HttpPost("{roleId}/permissions/{permissionId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignPermission(Guid roleId, Guid permissionId,
        CancellationToken cancellationToken)
    {
        try
        {
            var role = await _context.Roles.FindAsync(new object[] { roleId }, cancellationToken);
            var permission = await _context.Permissions.FindAsync(new object[] { permissionId }, cancellationToken);
            if (role == null || permission == null)
                return NotFound();

            var existing = await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId, cancellationToken);
            if (existing != null)
                return BadRequest("Permission already assigned to this role.");

            _context.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permissionId });
            await _context.SaveChangesAsync(cancellationToken);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning permission to role");
            return StatusCode(500,
                new { error = "An internal error occurred.", traceId = HttpContext.TraceIdentifier });
        }
    }

    /// <summary>
    ///     Удаляет право у роли
    /// </summary>
    [HttpDelete("{roleId}/permissions/{permissionId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePermission(Guid roleId, Guid permissionId,
        CancellationToken cancellationToken)
    {
        try
        {
            var rp = await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId, cancellationToken);
            if (rp == null)
                return NotFound();

            _context.RolePermissions.Remove(rp);
            await _context.SaveChangesAsync(cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing permission from role");
            return StatusCode(500,
                new { error = "An internal error occurred.", traceId = HttpContext.TraceIdentifier });
        }
    }
}