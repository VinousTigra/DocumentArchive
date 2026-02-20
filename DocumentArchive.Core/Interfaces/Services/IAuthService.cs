using DocumentArchive.Core.DTOs.Auth;

namespace DocumentArchive.Core.Interfaces.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto, string deviceInfo, string ipAddress);
    Task<AuthResponseDto> LoginAsync(LoginDto loginDto, string deviceInfo, string ipAddress);
    Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto, string deviceInfo, string ipAddress);
    Task LogoutAsync(Guid userId, string? ipAddress = null, string? deviceInfo = null);
    Task RevokeTokenAsync(string refreshToken, string? ipAddress = null, string? deviceInfo = null);
    Task<AuthResponseDto> GetProfileAsync(Guid userId);
    Task ForgotPasswordAsync(ForgotPasswordDto dto, string ipAddress, string deviceInfo);
    Task ResetPasswordAsync(ResetPasswordDto dto, string ipAddress, string deviceInfo);
    Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto, string? ipAddress = null, string? deviceInfo = null);
}