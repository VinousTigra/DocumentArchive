using AutoMapper;
using AutoMapper.QueryableExtensions;
using DocumentArchive.Core.DTOs.Document;
using DocumentArchive.Core.DTOs.Shared;
using DocumentArchive.Core.DTOs.Statistics;
using DocumentArchive.Core.DTOs.User;
using DocumentArchive.Core.Interfaces.Services;
using DocumentArchive.Core.Models;
using DocumentArchive.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocumentArchive.Services.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<UserService> _logger;

    public UserService(
        AppDbContext context,
        IMapper mapper,
        ILogger<UserService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResult<UserListItemDto>> GetUsersAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting users page {Page} size {PageSize}, search '{Search}'", page, pageSize, search);

        var query = _context.Users
            .AsNoTracking()
            .Where(u => !u.IsDeleted) // предположим, что мягко удалённых не показываем
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u => u.Username.Contains(search) ||
                                     u.Email.Contains(search) ||
                                     (u.FirstName != null && u.FirstName.Contains(search)) ||
                                     (u.LastName != null && u.LastName.Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(u => u.Username)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ProjectTo<UserListItemDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return new PagedResult<UserListItemDto>
        {
            Items = items,
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<UserResponseDto?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);

        return user == null ? null : _mapper.Map<UserResponseDto>(user);
    }

    public async Task<UserResponseDto> CreateUserAsync(CreateUserDto createDto, CancellationToken cancellationToken = default)
    {
        // Проверка уникальности email и username
        var emailExists = await _context.Users
            .AnyAsync(u => u.Email == createDto.Email, cancellationToken);
        if (emailExists)
            throw new InvalidOperationException($"User with email '{createDto.Email}' already exists.");

        var usernameExists = await _context.Users
            .AnyAsync(u => u.Username == createDto.Username, cancellationToken);
        if (usernameExists)
            throw new InvalidOperationException($"User with username '{createDto.Username}' already exists.");

        var user = _mapper.Map<User>(createDto);
        user.Id = Guid.NewGuid();
        user.CreatedAt = DateTime.UtcNow;
        user.IsActive = true;
        user.IsDeleted = false;

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} created", user.Id);
        return _mapper.Map<UserResponseDto>(user);
    }

    public async Task UpdateUserAsync(Guid id, UpdateUserDto updateDto, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
        if (user == null)
            throw new KeyNotFoundException($"User with id {id} not found");

        // Проверка уникальности email, если он меняется
        if (!string.IsNullOrWhiteSpace(updateDto.Email) && updateDto.Email != user.Email)
        {
            var emailExists = await _context.Users
                .AnyAsync(u => u.Email == updateDto.Email && u.Id != id, cancellationToken);
            if (emailExists)
                throw new InvalidOperationException($"User with email '{updateDto.Email}' already exists.");
        }

        // Проверка уникальности username, если он меняется
        if (!string.IsNullOrWhiteSpace(updateDto.Username) && updateDto.Username != user.Username)
        {
            var usernameExists = await _context.Users
                .AnyAsync(u => u.Username == updateDto.Username && u.Id != id, cancellationToken);
            if (usernameExists)
                throw new InvalidOperationException($"User with username '{updateDto.Username}' already exists.");
        }

        _mapper.Map(updateDto, user);
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("User {UserId} updated", id);
    }

    public async Task DeleteUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .Include(u => u.Documents) // для проверки наличия документов
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
        if (user == null)
            throw new KeyNotFoundException($"User with id {id} not found");

        if (user.Documents.Any())
            throw new InvalidOperationException("Cannot delete user with existing documents.");

        // Soft delete или реальное удаление? По заданию в модели есть IsDeleted, делаем soft delete
        user.IsDeleted = true;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("User {UserId} deleted (soft)", id);
    }

    public async Task<PagedResult<DocumentListItemDto>> GetUserDocumentsAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var userExists = await _context.Users
            .AnyAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);
        if (!userExists)
            throw new KeyNotFoundException($"User with id {userId} not found");

        var query = _context.Documents
            .AsNoTracking()
            .Where(d => d.UserId == userId)
            .OrderBy(d => d.Title);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ProjectTo<DocumentListItemDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return new PagedResult<DocumentListItemDto>
        {
            Items = items,
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
    
    public async Task<UserStatisticsDto> GetUserStatisticsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .Where(u => u.Id == userId && !u.IsDeleted)
            .Select(u => new UserStatisticsDto
            {
                UserId = u.Id,
                Username = u.Username,
                Email = u.Email,
                DocumentsCount = u.Documents.Count,
                LastLoginAt = u.LastLoginAt,
                RegisteredAt = u.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
            throw new KeyNotFoundException($"User with id {userId} not found");

        return user;
    }

    public async Task<UsersGeneralStatisticsDto> GetUsersGeneralStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var totalUsers = await _context.Users.CountAsync(u => !u.IsDeleted, cancellationToken);
        var activeToday = await _context.Users
            .CountAsync(u => u.LastLoginAt >= DateTime.UtcNow.Date, cancellationToken);
        var usersByDate = await _context.Users
            .Where(u => !u.IsDeleted)
            .GroupBy(u => u.CreatedAt.Date)
            .Select(g => new DateCountDto { Date = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return new UsersGeneralStatisticsDto
        {
            TotalUsers = totalUsers,
            ActiveToday = activeToday,
            UsersByRegistrationDate = usersByDate
        };
    }
    public async Task AssignRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        // Проверяем существование пользователя
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);
        if (user == null)
            throw new KeyNotFoundException($"User with id {userId} not found.");

        // Проверяем существование роли
        var role = await _context.Roles
            .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);
        if (role == null)
            throw new KeyNotFoundException($"Role with id {roleId} not found.");

        // Проверяем, не назначена ли уже роль
        var existing = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);
        if (existing != null)
            throw new InvalidOperationException("User already has this role.");

        // Создаём связь
        var userRole = new UserRole
        {
            UserId = userId,
            RoleId = roleId
        };
        _context.UserRoles.Add(userRole);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Role {RoleId} assigned to user {UserId}", roleId, userId);
    }

    public async Task RemoveRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var userRole = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);
        if (userRole == null)
            throw new KeyNotFoundException("User does not have this role.");

        _context.UserRoles.Remove(userRole);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Role {RoleId} removed from user {UserId}", roleId, userId);
    }
    
}