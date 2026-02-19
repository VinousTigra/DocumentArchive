using AutoMapper;
using DocumentArchive.Core.DTOs.Permission;
using DocumentArchive.Core.Models;
using DocumentArchive.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocumentArchive.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class PermissionsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<PermissionsController> _logger;
    private readonly IMapper _mapper;

    public PermissionsController(AppDbContext context, IMapper mapper, ILogger<PermissionsController> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    ///     Получает список всех прав
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<PermissionListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<PermissionListItemDto>>> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var permissions = await _context.Permissions
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .ToListAsync(cancellationToken);
            return Ok(_mapper.Map<List<PermissionListItemDto>>(permissions));
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request cancelled by client");
            return StatusCode(499, "Request cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permissions");
            return StatusCode(500,
                new { error = "An internal error occurred.", traceId = HttpContext.TraceIdentifier });
        }
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
        try
        {
            var permission = await _context.Permissions
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
            if (permission == null)
                return NotFound();
            return Ok(_mapper.Map<PermissionResponseDto>(permission));
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request cancelled by client");
            return StatusCode(499, "Request cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permission by ID {PermissionId}", id);
            return StatusCode(500,
                new { error = "An internal error occurred.", traceId = HttpContext.TraceIdentifier });
        }
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
        try
        {
            var exists = await _context.Permissions.AnyAsync(p => p.Name == dto.Name, cancellationToken);
            if (exists)
                return BadRequest($"Permission with name '{dto.Name}' already exists.");

            var permission = _mapper.Map<Permission>(dto);
            permission.Id = Guid.NewGuid();
            permission.CreatedAt = DateTime.UtcNow;

            _context.Permissions.Add(permission);
            await _context.SaveChangesAsync(cancellationToken);

            var response = _mapper.Map<PermissionResponseDto>(permission);
            return CreatedAtAction(nameof(GetById), new { id = permission.Id }, response);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request cancelled by client");
            return StatusCode(499, "Request cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating permission");
            return StatusCode(500,
                new { error = "An internal error occurred.", traceId = HttpContext.TraceIdentifier });
        }
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
        try
        {
            var permission = await _context.Permissions.FindAsync(new object[] { id }, cancellationToken);
            if (permission == null)
                return NotFound();

            if (!string.IsNullOrWhiteSpace(dto.Name) && dto.Name != permission.Name)
            {
                var exists =
                    await _context.Permissions.AnyAsync(p => p.Name == dto.Name && p.Id != id, cancellationToken);
                if (exists)
                    return BadRequest($"Permission with name '{dto.Name}' already exists.");
            }

            _mapper.Map(dto, permission);
            permission.UpdatedAt = DateTime.UtcNow;

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
            _logger.LogError(ex, "Error updating permission {PermissionId}", id);
            return StatusCode(500,
                new { error = "An internal error occurred.", traceId = HttpContext.TraceIdentifier });
        }
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
        try
        {
            var permission = await _context.Permissions
                .Include(p => p.RolePermissions)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
            if (permission == null)
                return NotFound();

            if (permission.RolePermissions.Any())
                return BadRequest("Cannot delete permission because it is assigned to roles.");

            _context.Permissions.Remove(permission);
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
            _logger.LogError(ex, "Error deleting permission {PermissionId}", id);
            return StatusCode(500,
                new { error = "An internal error occurred.", traceId = HttpContext.TraceIdentifier });
        }
    }
}