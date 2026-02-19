using DocumentArchive.Core.DTOs.Permission;

namespace DocumentArchive.Core.Interfaces.Services;

public interface IPermissionService
{
    Task<List<PermissionListItemDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PermissionResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PermissionResponseDto> CreateAsync(CreatePermissionDto dto, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, UpdatePermissionDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}