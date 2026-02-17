using DocumentArchive.Core.DTOs.Shared;
using DocumentArchive.Core.Models;

namespace DocumentArchive.Core.Interfaces.Repositorys;

public interface IUserRepository
{
    Task<PagedResult<User>> GetPagedAsync(int page, int pageSize, string? search, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task AddAsync(User entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(User entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}