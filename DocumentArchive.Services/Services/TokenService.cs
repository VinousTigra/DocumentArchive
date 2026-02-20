using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DocumentArchive.Core.Interfaces.Services;
using DocumentArchive.Core.Models;
using DocumentArchive.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace DocumentArchive.Services.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _context;
    private readonly ILogger<TokenService> _logger;

    public TokenService(IConfiguration configuration, AppDbContext context, ILogger<TokenService> logger)
    {
        _configuration = configuration;
        _context = context;
        _logger = logger;
    }

    public string GenerateAccessToken(User user, IList<string> roles, IList<string> permissions)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"];
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];
        var expirationMinutes = int.Parse(jwtSettings["AccessTokenExpirationMinutes"] ?? "15");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new("firstName", user.FirstName ?? ""),
            new("lastName", user.LastName ?? ""),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        foreach (var permission in permissions)
            claims.Add(new Claim("permission", permission));

        secretKey = jwtSettings["SecretKey"];
        if (string.IsNullOrEmpty(secretKey))
            throw new InvalidOperationException("JWT SecretKey is not configured.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: creds);

        if (user.DateOfBirth.HasValue)
            claims.Add(new Claim("DateOfBirth", user.DateOfBirth.Value.ToString("yyyy-MM-dd")));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<string> GenerateRefreshTokenAsync(Guid userId, string deviceInfo, string ipAddress)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var expirationDays = int.Parse(jwtSettings["RefreshTokenExpirationDays"] ?? "7");

        var refreshToken = GenerateSecureRefreshToken();
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RefreshTokenHash = BCrypt.Net.BCrypt.HashPassword(refreshToken),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(expirationDays),
            DeviceInfo = deviceInfo,
            IpAddress = ipAddress,
            IsRevoked = false
        };

        _context.UserSessions.Add(session);
        await _context.SaveChangesAsync();

        return refreshToken;
    }

    public async Task<UserSession?> ValidateRefreshTokenAsync(Guid userId, string refreshToken)
    {
        var activeSessions = await _context.UserSessions
            .Where(s => s.UserId == userId && !s.IsRevoked && s.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        foreach (var session in activeSessions)
            if (BCrypt.Net.BCrypt.Verify(refreshToken, session.RefreshTokenHash))
                return session;
        return null;
    }

    public async Task RevokeRefreshTokenAsync(Guid sessionId)
    {
        var session = await _context.UserSessions.FindAsync(sessionId);
        if (session != null)
        {
            session.IsRevoked = true;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Refresh token {SessionId} revoked", sessionId);
        }
    }

    public async Task RevokeAllUserSessionsAsync(Guid userId)
    {
        var sessions = await _context.UserSessions
            .Where(s => s.UserId == userId && !s.IsRevoked)
            .ToListAsync();
        foreach (var session in sessions) session.IsRevoked = true;
        await _context.SaveChangesAsync();
        _logger.LogInformation("All sessions revoked for user {UserId}", userId);
    }

    public DateTime GetAccessTokenExpiry()
    {
        var minutes = int.Parse(_configuration.GetSection("JwtSettings")["AccessTokenExpirationMinutes"] ?? "15");
        return DateTime.UtcNow.AddMinutes(minutes);
    }

    public DateTime GetRefreshTokenExpiry()
    {
        var days = int.Parse(_configuration.GetSection("JwtSettings")["RefreshTokenExpirationDays"] ?? "7");
        return DateTime.UtcNow.AddDays(days);
    }

    private string GenerateSecureRefreshToken()
    {
        var randomBytes = new byte[64];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        return Convert.ToBase64String(randomBytes);
    }
}