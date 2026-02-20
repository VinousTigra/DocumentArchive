using DocumentArchive.Core.DTOs.Auth;

namespace DocumentArchive.Core.Interfaces.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto, string deviceInfo, string ipAddress);
    Task<AuthResponseDto> LoginAsync(LoginDto loginDto, string deviceInfo, string ipAddress);
    Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto, string deviceInfo, string ipAddress);
    Task LogoutAsync(Guid userId);
    Task RevokeTokenAsync(string refreshToken);
}