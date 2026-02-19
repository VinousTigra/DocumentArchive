using AutoMapper;
using DocumentArchive.Core.DTOs.Role;
using DocumentArchive.Core.Interfaces.Services;
using DocumentArchive.Core.Models;
using DocumentArchive.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocumentArchive.Services.Services;

public class RoleService : IRoleService
{
    private readonly AppDbContext _context;
    private readonly ILogger<RoleService> _logger;
    private readonly IMapper _mapper;

    public RoleService(AppDbContext context, IMapper mapper, ILogger<RoleService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<List<RoleListItemDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var roles = await _context.Roles
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
        return _mapper.Map<List<RoleListItemDto>>(roles);
    }

    public async Task<RoleResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var role = await _context.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        return role == null ? null : _mapper.Map<RoleResponseDto>(role);
    }

    public async Task<RoleResponseDto> CreateAsync(CreateRoleDto dto, CancellationToken cancellationToken)
    {
        // Проверка уникальности имени
        var exists = await _context.Roles.AnyAsync(r => r.Name == dto.Name, cancellationToken);
        if (exists)
            throw new InvalidOperationException($"Role with name '{dto.Name}' already exists.");

        var role = _mapper.Map<Role>(dto);
        role.Id = Guid.NewGuid();
        role.CreatedAt = DateTime.UtcNow;

        _context.Roles.Add(role);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Role {RoleId} created", role.Id);
        return _mapper.Map<RoleResponseDto>(role);
    }

    public async Task UpdateAsync(Guid id, UpdateRoleDto dto, CancellationToken cancellationToken)
    {
        var role = await _context.Roles.FindAsync(new object[] { id }, cancellationToken);
        if (role == null)
            throw new KeyNotFoundException($"Role with id {id} not found.");

        // Проверка уникальности имени, если оно меняется
        if (!string.IsNullOrWhiteSpace(dto.Name) && dto.Name != role.Name)
        {
            var exists = await _context.Roles.AnyAsync(r => r.Name == dto.Name && r.Id != id, cancellationToken);
            if (exists)
                throw new InvalidOperationException($"Role with name '{dto.Name}' already exists.");
        }

        _mapper.Map(dto, role);
        role.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Role {RoleId} updated", id);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var role = await _context.Roles
            .Include(r => r.UserRoles)
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (role == null)
            throw new KeyNotFoundException($"Role with id {id} not found.");

        if (role.UserRoles.Any() || role.RolePermissions.Any())
            throw new InvalidOperationException("Cannot delete role because it has assigned users or permissions.");

        _context.Roles.Remove(role);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Role {RoleId} deleted", id);
    }

    public async Task AssignPermissionAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken)
    {
        var role = await _context.Roles.FindAsync(new object[] { roleId }, cancellationToken);
        var permission = await _context.Permissions.FindAsync(new object[] { permissionId }, cancellationToken);
        if (role == null || permission == null)
            throw new KeyNotFoundException("Role or permission not found.");

        var existing = await _context.RolePermissions
            .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId, cancellationToken);
        if (existing != null)
            throw new InvalidOperationException("Permission already assigned to this role.");

        _context.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permissionId });
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Permission {PermissionId} assigned to role {RoleId}", permissionId, roleId);
    }

    public async Task RemovePermissionAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken)
    {
        var rp = await _context.RolePermissions
            .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId, cancellationToken);
        if (rp == null)
            throw new KeyNotFoundException("Permission not assigned to this role.");

        _context.RolePermissions.Remove(rp);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Permission {PermissionId} removed from role {RoleId}", permissionId, roleId);
    }
}