using DocumentArchive.Core.DTOs.Shared;
using DocumentArchive.Core.Interfaces.Repositorys;
using DocumentArchive.Core.Models;
using DocumentArchive.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace DocumentArchive.Infrastructure.Repositories;

public class UserRepository : FileStorageRepository<User>, IUserRepository
{
    // Для тестов
    public UserRepository(string? dataDirectory = null) 
        : base("users.json", u => u.Id, dataDirectory)
    {
    }

    // Для DI
    public UserRepository(IOptions<StorageOptions> options) 
        : base("users.json", u => u.Id, options)
    {
    }

    public Task<PagedResult<User>> GetPagedAsync(int page, int pageSize, string? search)
    {
        throw new NotImplementedException();
    }

    public async Task<User?> FindByEmailAsync(string email)
    {
        return (await FindAsync(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase))).FirstOrDefault();
    }

    public async Task<User?> FindByUsernameAsync(string username)
    {
        return (await FindAsync(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase))).FirstOrDefault();
    }

    public Task<PagedResult<User>> GetPagedAsync(int page, int pageSize, string? search, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
    
}