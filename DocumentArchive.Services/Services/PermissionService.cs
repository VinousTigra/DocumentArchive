using AutoMapper;
using DocumentArchive.Core.DTOs.Permission;
using DocumentArchive.Core.Interfaces.Services;
using DocumentArchive.Core.Models;
using DocumentArchive.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocumentArchive.Services.Services;

public class PermissionService : IPermissionService
{
    private readonly AppDbContext _context;
    private readonly ILogger<PermissionService> _logger;
    private readonly IMapper _mapper;

    public PermissionService(AppDbContext context, IMapper mapper, ILogger<PermissionService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<List<PermissionListItemDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var permissions = await _context.Permissions
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
        return _mapper.Map<List<PermissionListItemDto>>(permissions);
    }

    public async Task<PermissionResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var permission = await _context.Permissions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        return permission == null ? null : _mapper.Map<PermissionResponseDto>(permission);
    }

    public async Task<PermissionResponseDto> CreateAsync(CreatePermissionDto dto, CancellationToken cancellationToken)
    {
        // Проверка уникальности имени
        var exists = await _context.Permissions.AnyAsync(p => p.Name == dto.Name, cancellationToken);
        if (exists)
            throw new InvalidOperationException($"Permission with name '{dto.Name}' already exists.");

        var permission = _mapper.Map<Permission>(dto);
        permission.Id = Guid.NewGuid();
        permission.CreatedAt = DateTime.UtcNow;

        _context.Permissions.Add(permission);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Permission {PermissionId} created", permission.Id);
        return _mapper.Map<PermissionResponseDto>(permission);
    }

    public async Task UpdateAsync(Guid id, UpdatePermissionDto dto, CancellationToken cancellationToken)
    {
        var permission = await _context.Permissions.FindAsync(new object[] { id }, cancellationToken);
        if (permission == null)
            throw new KeyNotFoundException($"Permission with id {id} not found.");

        // Проверка уникальности имени, если оно меняется
        if (!string.IsNullOrWhiteSpace(dto.Name) && dto.Name != permission.Name)
        {
            var exists = await _context.Permissions.AnyAsync(p => p.Name == dto.Name && p.Id != id, cancellationToken);
            if (exists)
                throw new InvalidOperationException($"Permission with name '{dto.Name}' already exists.");
        }

        _mapper.Map(dto, permission);
        permission.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Permission {PermissionId} updated", id);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var permission = await _context.Permissions
            .Include(p => p.RolePermissions)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (permission == null)
            throw new KeyNotFoundException($"Permission with id {id} not found.");

        if (permission.RolePermissions.Any())
            throw new InvalidOperationException("Cannot delete permission because it is assigned to roles.");

        _context.Permissions.Remove(permission);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Permission {PermissionId} deleted", id);
    }
}