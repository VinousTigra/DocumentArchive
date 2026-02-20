using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using DocumentArchive.Core.DTOs.Auth;
using DocumentArchive.Core.Interfaces.Services;
using DocumentArchive.Core.Models;
using DocumentArchive.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace DocumentArchive.Services.Services;

public class AuthService : IAuthService
{
    private readonly IAuditService _auditService;
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _context;
    private readonly ILogger<AuthService> _logger;
    private readonly IMapper _mapper;
    private readonly ITokenService _tokenService;
    private readonly TokenValidationParameters _tokenValidationParameters;

    public AuthService(
        AppDbContext context,
        ITokenService tokenService,
        IMapper mapper,
        ILogger<AuthService> logger,
        IConfiguration configuration,
        IAuditService auditService)
    {
        _context = context;
        _tokenService = tokenService;
        _mapper = mapper;
        _logger = logger;
        _configuration = configuration;
        _auditService = auditService;
        _configuration = configuration;
        _auditService = auditService;

        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"];
        _tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false, // не проверяем срок
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero
        };
    }


    #region Helper Methods

    private string GenerateSecureToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    #endregion

    #region IAuthService Implementation

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto, string deviceInfo, string ipAddress)
    {
        try
        {
            // Проверка уникальности
            if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email))
                throw new InvalidOperationException("Email already registered");
            if (await _context.Users.AnyAsync(u => u.Username == registerDto.Username))
                throw new InvalidOperationException("Username already taken");

            // Хеширование пароля
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);

            var user = _mapper.Map<User>(registerDto);
            user.Id = Guid.NewGuid();
            user.PasswordHash = passwordHash;
            user.CreatedAt = DateTime.UtcNow;
            user.IsActive = true;
            user.IsEmailConfirmed = false;
            user.IsDeleted = false;

            _context.Users.Add(user);

            // Назначение роли "User" по умолчанию
            var userRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "User");
            if (userRole == null)
                throw new InvalidOperationException("Default role 'User' not found. Ensure seed data is present.");

            _context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = userRole.Id });

            // Генерация токена подтверждения email
            var confirmationToken = GenerateSecureToken();
            var emailToken = new EmailConfirmationToken
            {
                UserId = user.Id,
                TokenHash = BCrypt.Net.BCrypt.HashPassword(confirmationToken),
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };
            _context.EmailConfirmationTokens.Add(emailToken);

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} registered", user.Id);

            // Логирование успеха
            await _auditService.LogAsync(
                SecurityEventType.Register,
                user.Id,
                user.Email,
                ipAddress,
                deviceInfo,
                true,
                new { user.Username });

            return new AuthResponseDto
            {
                UserId = user.Id,
                Email = user.Email,
                Username = user.Username,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = new List<string> { "User" },
                ConfirmationToken = confirmationToken // для тестирования
            };
        }
        catch (InvalidOperationException ex)
        {
            await _auditService.LogAsync(
                SecurityEventType.Register,
                null,
                registerDto.Email,
                ipAddress,
                deviceInfo,
                false,
                new { Reason = ex.Message });
            throw;
        }
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto, string deviceInfo, string ipAddress)
    {
        User? user = null;
        try
        {
            user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u =>
                    u.Email == loginDto.EmailOrUsername || u.Username == loginDto.EmailOrUsername);

            if (user == null || !user.IsActive || user.IsDeleted)
            {
                await _auditService.LogAsync(
                    SecurityEventType.FailedLogin,
                    null,
                    loginDto.EmailOrUsername,
                    ipAddress,
                    deviceInfo,
                    false,
                    new { Reason = "User not found or inactive" });
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                await _auditService.LogAsync(
                    SecurityEventType.FailedLogin,
                    user.Id,
                    user.Email,
                    ipAddress,
                    deviceInfo,
                    false,
                    new { Reason = "Wrong password" });
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

            // Получение прав через роли
            var permissions = await _context.RolePermissions
                .Where(rp => user.UserRoles.Select(ur => ur.RoleId).Contains(rp.RoleId))
                .Select(rp => rp.Permission.Name)
                .Distinct()
                .ToListAsync();

            var accessToken = _tokenService.GenerateAccessToken(user, roles, permissions);
            var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user.Id, deviceInfo, ipAddress);

            await _auditService.LogAsync(
                SecurityEventType.Login,
                user.Id,
                user.Email,
                ipAddress,
                deviceInfo,
                true,
                new { Roles = roles });

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiry = _tokenService.GetAccessTokenExpiry(),
                RefreshTokenExpiry = _tokenService.GetRefreshTokenExpiry(),
                UserId = user.Id,
                Email = user.Email,
                Username = user.Username,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = roles
            };
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            await _auditService.LogAsync(
                SecurityEventType.FailedLogin,
                user?.Id,
                user?.Email ?? loginDto.EmailOrUsername,
                ipAddress,
                deviceInfo,
                false,
                new { Reason = ex.Message });
            throw;
        }
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto, string deviceInfo,
        string ipAddress)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            ClaimsPrincipal principal;
            try
            {
                principal = handler.ValidateToken(refreshTokenDto.AccessToken, _tokenValidationParameters,
                    out var validatedToken);
            }
            catch (SecurityTokenException)
            {
                await _auditService.LogAsync(SecurityEventType.TokenRefresh, null, null, ipAddress, deviceInfo, false,
                    new { Reason = "Invalid access token signature" });
                throw new UnauthorizedAccessException("Invalid token");
            }

            var userIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                await _auditService.LogAsync(SecurityEventType.TokenRefresh, null, null, ipAddress, deviceInfo, false,
                    new { Reason = "Invalid access token claims" });
                throw new UnauthorizedAccessException("Invalid token");
            }


            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive && !u.IsDeleted);
            if (user == null)
            {
                await _auditService.LogAsync(
                    SecurityEventType.TokenRefresh,
                    userId,
                    null,
                    ipAddress,
                    deviceInfo,
                    false,
                    new { Reason = "User not found or inactive" });
                throw new UnauthorizedAccessException("User not found");
            }

            var session = await _tokenService.ValidateRefreshTokenAsync(userId, refreshTokenDto.RefreshToken);
            if (session == null)
            {
                await _auditService.LogAsync(
                    SecurityEventType.TokenRefresh,
                    userId,
                    user.Email,
                    ipAddress,
                    deviceInfo,
                    false,
                    new { Reason = "Invalid refresh token" });
                throw new UnauthorizedAccessException("Invalid refresh token");
            }

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

            await _auditService.LogAsync(
                SecurityEventType.TokenRefresh,
                userId,
                user.Email,
                ipAddress,
                deviceInfo,
                true,
                new { OldSessionId = session.Id });

            return new AuthResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                AccessTokenExpiry = _tokenService.GetAccessTokenExpiry(),
                RefreshTokenExpiry = _tokenService.GetRefreshTokenExpiry(),
                UserId = user.Id,
                Email = user.Email,
                Username = user.Username,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = roles
            };
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            await _auditService.LogAsync(
                SecurityEventType.TokenRefresh,
                null,
                null,
                ipAddress,
                deviceInfo,
                false,
                new { Reason = ex.Message });
            throw;
        }
    }

    public async Task LogoutAsync(Guid userId, string? ipAddress = null, string? deviceInfo = null)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            await _auditService.LogAsync(
                SecurityEventType.Logout,
                userId,
                null,
                ipAddress,
                deviceInfo,
                false,
                new { Reason = "User not found" });
            throw new KeyNotFoundException("User not found");
        }

        await _tokenService.RevokeAllUserSessionsAsync(userId);

        await _auditService.LogAsync(
            SecurityEventType.Logout,
            userId,
            user.Email,
            ipAddress,
            deviceInfo,
            true);
    }

    public async Task RevokeTokenAsync(string refreshToken, string? ipAddress = null, string? deviceInfo = null)
    {
        // Поиск сессии по токену (хеш)
        var sessions = await _context.UserSessions
            .Include(s => s.User)
            .Where(s => !s.IsRevoked && s.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();
        var session = sessions.FirstOrDefault(s => BCrypt.Net.BCrypt.Verify(refreshToken, s.RefreshTokenHash));
        if (session == null)
        {
            await _auditService.LogAsync(
                SecurityEventType.TokenRevoke,
                null,
                null,
                ipAddress,
                deviceInfo,
                false,
                new { Reason = "Refresh token not found or expired" });
            throw new KeyNotFoundException("Refresh token not found or already expired");
        }

        session.IsRevoked = true;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            SecurityEventType.TokenRevoke,
            session.UserId,
            session.User?.Email,
            ipAddress,
            deviceInfo,
            true,
            new { SessionId = session.Id });
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

    public async Task ForgotPasswordAsync(ForgotPasswordDto dto, string ipAddress, string deviceInfo)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null)
        {
            // Не раскрываем существование пользователя, но логируем
            await _auditService.LogAsync(
                SecurityEventType.PasswordResetRequested,
                null,
                dto.Email,
                ipAddress,
                deviceInfo,
                false,
                new { Reason = "Email not found" });
            _logger.LogInformation("Password reset requested for non-existent email {Email} from IP {IpAddress}",
                dto.Email, ipAddress);
            return;
        }

        // Отзываем предыдущие неиспользованные токены
        var existingTokens = await _context.PasswordResetTokens
            .Where(t => t.UserId == user.Id && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();
        foreach (var token in existingTokens) token.IsUsed = true; // или удалить

        var resetToken = GenerateSecureToken();
        var tokenEntity = new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = BCrypt.Net.BCrypt.HashPassword(resetToken),
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };
        _context.PasswordResetTokens.Add(tokenEntity);
        await _context.SaveChangesAsync();

        // Логируем успешную генерацию токена
        await _auditService.LogAsync(
            SecurityEventType.PasswordResetRequested,
            user.Id,
            user.Email,
            ipAddress,
            deviceInfo,
            true,
            new { TokenExpiry = tokenEntity.ExpiresAt });

        // Здесь отправка email с resetToken
        _logger.LogInformation("Password reset token generated for user {UserId} from IP {IpAddress}", user.Id,
            ipAddress);
    }

    public async Task ResetPasswordAsync(ResetPasswordDto dto, string ipAddress, string deviceInfo)
    {
        // Поиск действующего токена по хешу
        var allTokens = await _context.PasswordResetTokens
            .Include(t => t.User)
            .Where(t => !t.IsUsed && t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();
        var tokenEntity = allTokens.FirstOrDefault(t => BCrypt.Net.BCrypt.Verify(dto.Token, t.TokenHash));
        if (tokenEntity == null)
        {
            await _auditService.LogAsync(
                SecurityEventType.PasswordReset,
                null,
                null,
                ipAddress,
                deviceInfo,
                false,
                new { Reason = "Invalid or expired token" });
            throw new InvalidOperationException("Invalid or expired reset token");
        }

        var user = tokenEntity.User;
        if (user == null)
        {
            await _auditService.LogAsync(
                SecurityEventType.PasswordReset,
                null,
                null,
                ipAddress,
                deviceInfo,
                false,
                new { Reason = "User not found" });
            throw new InvalidOperationException("User not found");
        }

        // Хешируем новый пароль
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        tokenEntity.IsUsed = true;
        await _tokenService.RevokeAllUserSessionsAsync(user.Id);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            SecurityEventType.PasswordReset,
            user.Id,
            user.Email,
            ipAddress,
            deviceInfo,
            true);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto, string? ipAddress = null,
        string? deviceInfo = null)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            await _auditService.LogAsync(
                SecurityEventType.PasswordChange,
                userId,
                null,
                ipAddress,
                deviceInfo,
                false,
                new { Reason = "User not found" });
            throw new KeyNotFoundException("User not found");
        }

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
        {
            await _auditService.LogAsync(
                SecurityEventType.PasswordChange,
                userId,
                user.Email,
                ipAddress,
                deviceInfo,
                false,
                new { Reason = "Incorrect current password" });
            throw new UnauthorizedAccessException("Current password is incorrect");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _tokenService
            .RevokeAllUserSessionsAsync(
                userId); // опционально, можно оставить текущую сессию, но для безопасности отзываем все
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            SecurityEventType.PasswordChange,
            userId,
            user.Email,
            ipAddress,
            deviceInfo,
            true);
    }

    public async Task ConfirmEmailAsync(ConfirmEmailDto dto, string ipAddress, string deviceInfo)
    {
        // Поиск действующего токена подтверждения
        var allTokens = await _context.EmailConfirmationTokens
            .Include(t => t.User)
            .Where(t => !t.IsUsed && t.ExpiresAt > DateTime.UtcNow && t.UserId == dto.UserId)
            .ToListAsync();
        var tokenEntity = allTokens.FirstOrDefault(t => BCrypt.Net.BCrypt.Verify(dto.Token, t.TokenHash));
        if (tokenEntity == null)
        {
            await _auditService.LogAsync(
                SecurityEventType.EmailConfirmed,
                dto.UserId,
                null,
                ipAddress,
                deviceInfo,
                false,
                new { Reason = "Invalid or expired token" });
            throw new InvalidOperationException("Invalid or expired confirmation token");
        }

        var user = tokenEntity.User;
        if (user == null)
        {
            await _auditService.LogAsync(
                SecurityEventType.EmailConfirmed,
                dto.UserId,
                null,
                ipAddress,
                deviceInfo,
                false,
                new { Reason = "User not found" });
            throw new KeyNotFoundException("User not found");
        }

        if (user.IsEmailConfirmed)
        {
            await _auditService.LogAsync(
                SecurityEventType.EmailConfirmed,
                user.Id,
                user.Email,
                ipAddress,
                deviceInfo,
                false,
                new { Reason = "Email already confirmed" });
            throw new InvalidOperationException("Email already confirmed");
        }

        user.IsEmailConfirmed = true;
        user.UpdatedAt = DateTime.UtcNow;
        tokenEntity.IsUsed = true;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            SecurityEventType.EmailConfirmed,
            user.Id,
            user.Email,
            ipAddress,
            deviceInfo,
            true);
    }

    #endregion
}