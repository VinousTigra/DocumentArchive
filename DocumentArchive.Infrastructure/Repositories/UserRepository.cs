using DocumentArchive.Core.Interfaces;
using DocumentArchive.Core.Models;

namespace DocumentArchive.Infrastructure.Repositories;

public class UserRepository : FileStorageRepository<User>, IUserRepository
{
    public UserRepository(string? dataDirectory = null)
        : base("users.json", u => u.Id, dataDirectory)
    {
    }

    public async Task<User?> FindByEmailAsync(string email)
    {
        return (await FindAsync(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase))).FirstOrDefault();
    }

    public async Task<User?> FindByUsernameAsync(string username)
    {
        return (await FindAsync(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase))).FirstOrDefault();
    }
}