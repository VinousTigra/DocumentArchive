namespace DocumentArchive.Core.DTOs.Auth;

public class RefreshTokenDto
{
    public string AccessToken { get; set; } = string.Empty; // истекший access token
    public string RefreshToken { get; set; } = string.Empty;
}