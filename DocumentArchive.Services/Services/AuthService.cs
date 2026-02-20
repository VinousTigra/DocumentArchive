using System.IdentityModel.Tokens.Jwt;
using AutoMapper;
using DocumentArchive.Core.DTOs.Auth;
using DocumentArchive.Core.Interfaces.Services;
using DocumentArchive.Core.Models;
using DocumentArchive.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DocumentArchive.Services.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IMapper _mapper;
    private readonly ILogger<AuthService> _logger;
    private readonly IConfiguration _configuration;

    public AuthService(
        AppDbContext context,
        ITokenService tokenService,
        IMapper mapper,
        ILogger<AuthService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _tokenService = tokenService;
        _mapper = mapper;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto, string deviceInfo, string ipAddress)
    {
        // Проверки уже выполнены валидатором, но дублируем для безопасности
        if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email))
            throw new InvalidOperationException("Email already registered");
        if (await _context.Users.AnyAsync(u => u.Username == registerDto.Username))
            throw new InvalidOperationException("Username already taken");

        // Хешируем пароль
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);

        var user = _mapper.Map<User>(registerDto);
        user.Id = Guid.NewGuid();
        user.PasswordHash = passwordHash;
        user.CreatedAt = DateTime.UtcNow;
        user.IsActive = true;
        user.IsEmailConfirmed = false;
        user.IsDeleted = false;

        _context.Users.Add(user);

        // Назначаем роль "User"
        var userRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "User");
        if (userRole == null)
            throw new InvalidOperationException("Default role 'User' not found. Ensure seed data is present.");

        _context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = userRole.Id });

        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} registered", user.Id);

        // Возвращаем ответ без токенов (как просят в задании)
        return new AuthResponseDto
        {
            UserId = user.Id,
            Email = user.Email,
            Username = user.Username,
            Roles = new List<string> { "User" },
            // Токены не выдаём
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto, string deviceInfo, string ipAddress)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == loginDto.EmailOrUsername || u.Username == loginDto.EmailOrUsername);

        if (user == null || !user.IsActive || user.IsDeleted)
            throw new UnauthorizedAccessException("Invalid credentials");

        if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials");

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

        // Получаем права через роли
        var permissions = await _context.RolePermissions
            .Where(rp => user.UserRoles.Select(ur => ur.RoleId).Contains(rp.RoleId))
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToListAsync();

        var accessToken = _tokenService.GenerateAccessToken(user, roles, permissions);
        var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user.Id, deviceInfo, ipAddress);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiry = _tokenService.GetAccessTokenExpiry(),
            RefreshTokenExpiry = _tokenService.GetRefreshTokenExpiry(),
            UserId = user.Id,
            Email = user.Email,
            Username = user.Username,
            Roles = roles
        };
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto, string deviceInfo, string ipAddress)
    {
        // Извлекаем userId из истекшего access token (без проверки срока)
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(refreshTokenDto.AccessToken);
        var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            throw new UnauthorizedAccessException("Invalid token");

        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive && !u.IsDeleted);
        if (user == null)
            throw new UnauthorizedAccessException("User not found");

        // Проверяем refresh token
        var session = await _tokenService.ValidateRefreshTokenAsync(userId, refreshTokenDto.RefreshToken);
        if (session == null)
            throw new UnauthorizedAccessException("Invalid refresh token");

        // Ротация: отзываем старую сессию
        await _tokenService.RevokeRefreshTokenAsync(session.Id);

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var permissions = await _context.RolePermissions
            .Where(rp => user.UserRoles.Select(ur => ur.RoleId).Contains(rp.RoleId))
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToListAsync();

        var newAccessToken = _tokenService.GenerateAccessToken(user, roles, permissions);
        var newRefreshToken = await _tokenService.GenerateRefreshTokenAsync(user.Id, deviceInfo, ipAddress);

        return new AuthResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            AccessTokenExpiry = _tokenService.GetAccessTokenExpiry(),
            RefreshTokenExpiry = _tokenService.GetRefreshTokenExpiry(),
            UserId = user.Id,
            Email = user.Email,
            Username = user.Username,
            Roles = roles
        };
    }

    public async Task LogoutAsync(Guid userId)
    {
        await _tokenService.RevokeAllUserSessionsAsync(userId);
    }

    public async Task RevokeTokenAsync(string refreshToken)
    {
        // Поиск сессии по токену (хеш)
        var sessions = await _context.UserSessions
            .Where(s => !s.IsRevoked && s.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();
        foreach (var session in sessions)
        {
            if (BCrypt.Net.BCrypt.Verify(refreshToken, session.RefreshTokenHash))
            {
                session.IsRevoked = true;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Refresh token {SessionId} revoked", session.Id);
                return;
            }
        }
        throw new KeyNotFoundException("Refresh token not found or already expired");
    }
    
    public async Task<AuthResponseDto> GetProfileAsync(Guid userId)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive && !u.IsDeleted);
        if (user == null)
            throw new KeyNotFoundException("User not found");

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

        return new AuthResponseDto
        {
            UserId = user.Id,
            Email = user.Email,
            Username = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Roles = roles
        };
    }
}