using DocumentArchive.Core.Models;

namespace DocumentArchive.Core.Interfaces.Services;

public interface ITokenService
{
    string GenerateAccessToken(User user, IList<string> roles, IList<string> permissions);
    Task<string> GenerateRefreshTokenAsync(Guid userId, string deviceInfo, string ipAddress);
    Task<UserSession?> ValidateRefreshTokenAsync(Guid userId, string refreshToken);
    Task RevokeRefreshTokenAsync(Guid sessionId);
    Task RevokeAllUserSessionsAsync(Guid userId);
}