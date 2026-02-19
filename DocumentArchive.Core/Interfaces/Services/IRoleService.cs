using DocumentArchive.Core.DTOs.Role;

namespace DocumentArchive.Core.Interfaces.Services;

public interface IRoleService
{
    Task<List<RoleListItemDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<RoleResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<RoleResponseDto> CreateAsync(CreateRoleDto dto, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, UpdateRoleDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task AssignPermissionAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default);
    Task RemovePermissionAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default);
}