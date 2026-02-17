using DocumentArchive.Core.DTOs.Document;
using DocumentArchive.Core.DTOs.Shared;
using DocumentArchive.Core.DTOs.User;

namespace DocumentArchive.Core.Interfaces.Services;

public interface IUserService
{
    Task<PagedResult<UserListItemDto>> GetUsersAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default);

    Task<UserResponseDto?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserResponseDto> CreateUserAsync(CreateUserDto createDto, CancellationToken cancellationToken = default);
    Task UpdateUserAsync(Guid id, UpdateUserDto updateDto, CancellationToken cancellationToken = default);
    Task DeleteUserAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Получает документы пользователя с пагинацией
    /// </summary>
    Task<PagedResult<DocumentListItemDto>> GetUserDocumentsAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}