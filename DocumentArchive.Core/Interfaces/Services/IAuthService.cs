using DocumentArchive.Core.DTOs.Auth;

namespace DocumentArchive.Core.Interfaces.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto, string deviceInfo, string ipAddress);
    Task<AuthResponseDto> LoginAsync(LoginDto loginDto, string deviceInfo, string ipAddress);
    Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto, string deviceInfo, string ipAddress);
    Task LogoutAsync(Guid userId);
    Task RevokeTokenAsync(string refreshToken);
    Task<AuthResponseDto> GetProfileAsync(Guid userId);
    
    
    Task ForgotPasswordAsync(ForgotPasswordDto dto, string ipAddress);
    Task ResetPasswordAsync(ResetPasswordDto dto, string ipAddress);
    Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
    Task ConfirmEmailAsync(ConfirmEmailDto dto);
}