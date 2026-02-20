using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
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
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _context;
    private readonly ILogger<AuthService> _logger;
    private readonly IMapper _mapper;
    private readonly ITokenService _tokenService;

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
        // Проверки уникальности (как раньше)
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

        // Генерация токена подтверждения email
        var confirmationToken = GenerateSecureToken(); // используем тот же метод, что и для сброса пароля
        var emailToken = new EmailConfirmationToken
        {
            UserId = user.Id,
            TokenHash = BCrypt.Net.BCrypt.HashPassword(confirmationToken),
            ExpiresAt = DateTime.UtcNow.AddDays(7) // срок действия 7 дней
        };
        _context.EmailConfirmationTokens.Add(emailToken);

        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} registered", user.Id);

        return new AuthResponseDto
        {
            UserId = user.Id,
            Email = user.Email,
            Username = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Roles = new List<string> { "User" },
            ConfirmationToken = confirmationToken // возвращаем токен для тестирования
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

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto, string deviceInfo,
        string ipAddress)
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
            if (BCrypt.Net.BCrypt.Verify(refreshToken, session.RefreshTokenHash))
            {
                session.IsRevoked = true;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Refresh token {SessionId} revoked", session.Id);
                return;
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

    public async Task ForgotPasswordAsync(ForgotPasswordDto dto, string ipAddress)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null)
        {
            // Не раскрываем существование пользователя
            _logger.LogInformation("Password reset requested for non-existent email {Email} from IP {IpAddress}",
                dto.Email, ipAddress);
            return;
        }

        // Отзываем все предыдущие неиспользованные токены для этого пользователя (опционально)
        var existingTokens = await _context.PasswordResetTokens
            .Where(t => t.UserId == user.Id && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();
        foreach (var token in existingTokens) token.IsUsed = true; // или удалить

        var resetToken = GenerateSecureToken();
        var tokenEntity = new PasswordResetToken
        {
            UserId = user.Id,
            Token = BCrypt.Net.BCrypt.HashPassword(resetToken), // храним хеш токена
            ExpiresAt = DateTime.UtcNow.AddHours(24) // токен действует 24 часа
        };
        _context.PasswordResetTokens.Add(tokenEntity);
        await _context.SaveChangesAsync();

        // Здесь должна быть отправка email с resetToken
        _logger.LogInformation("Password reset token generated for user {UserId} from IP {IpAddress}", user.Id,
            ipAddress);
    }

    public async Task ResetPasswordAsync(ResetPasswordDto dto, string ipAddress)
    {
        // Ищем токен в БД (сравниваем хеш)
        var allTokens = await _context.PasswordResetTokens
            .Include(t => t.User)
            .Where(t => !t.IsUsed && t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();
        var tokenEntity = allTokens.FirstOrDefault(t => BCrypt.Net.BCrypt.Verify(dto.Token, t.Token));
        if (tokenEntity == null)
            throw new InvalidOperationException("Invalid or expired reset token");

        var user = tokenEntity.User;
        if (user == null)
            throw new InvalidOperationException("User not found");

        // Хешируем новый пароль
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        // Помечаем токен как использованный
        tokenEntity.IsUsed = true;

        // Отзываем все refresh-токены пользователя
        await _tokenService.RevokeAllUserSessionsAsync(user.Id);

        await _context.SaveChangesAsync();
        _logger.LogInformation("Password reset for user {UserId} from IP {IpAddress}", user.Id, ipAddress);
    }


    public async Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new KeyNotFoundException("User not found");

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        // Опционально: отзываем все сессии, кроме текущей (если есть механизм идентификации сессии)
        await _tokenService.RevokeAllUserSessionsAsync(userId);

        await _context.SaveChangesAsync();
        _logger.LogInformation("Password changed for user {UserId}", userId);
    }

    public async Task ConfirmEmailAsync(ConfirmEmailDto dto)
    {
        var user = await _context.Users.FindAsync(dto.UserId);
        if (user == null)
            throw new KeyNotFoundException("User not found");

        if (user.IsEmailConfirmed)
            throw new InvalidOperationException("Email already confirmed");

        // Получаем все действующие токены для этого пользователя
        var validTokens = await _context.EmailConfirmationTokens
            .Where(t => t.UserId == dto.UserId && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        // Ищем токен, который совпадает с предоставленным (проверяем через BCrypt)
        var tokenEntity = validTokens.FirstOrDefault(t => BCrypt.Net.BCrypt.Verify(dto.Token, t.TokenHash));
        if (tokenEntity == null)
            throw new InvalidOperationException("Invalid or expired confirmation token");

        // Подтверждаем email
        user.IsEmailConfirmed = true;
        user.UpdatedAt = DateTime.UtcNow;

        // Помечаем токен как использованный
        tokenEntity.IsUsed = true;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Email confirmed for user {UserId}", user.Id);
    }

    private string GenerateSecureToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}