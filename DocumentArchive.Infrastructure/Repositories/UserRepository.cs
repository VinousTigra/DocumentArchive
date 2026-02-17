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

    public async Task<PagedResult<User>> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var users = await GetAllAsync(cancellationToken);

        // Поиск по Username или Email
        if (!string.IsNullOrWhiteSpace(search))
        {
            var lowerSearch = search.ToLowerInvariant();
            users = users.Where(u =>
                u.Username.ToLowerInvariant().Contains(lowerSearch) ||
                u.Email.ToLowerInvariant().Contains(lowerSearch));
        }

        // Сортировка по Username
        users = users.OrderBy(u => u.Username);

        // Пагинация
        var totalCount = users.Count();
        var items = users
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<User>
        {
            Items = items,
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return (await FindAsync(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase), cancellationToken))
            .FirstOrDefault();
    }

    public async Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return (await FindAsync(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase), cancellationToken))
            .FirstOrDefault();
    }
}