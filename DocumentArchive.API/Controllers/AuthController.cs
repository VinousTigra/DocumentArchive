using DocumentArchive.Core.DTOs.Auth;
using DocumentArchive.Core.Interfaces.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DocumentArchive.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IValidator<RegisterDto> _registerValidator;
    private readonly IValidator<LoginDto> _loginValidator;
    private readonly IValidator<ForgotPasswordDto> _forgotPasswordValidator;
    private readonly IValidator<ResetPasswordDto> _resetPasswordValidator;
    private readonly IValidator<ChangePasswordDto> _changePasswordValidator;
    private readonly IValidator<ConfirmEmailDto> _confirmEmailValidator;

    public AuthController(
        IAuthService authService,
        IValidator<RegisterDto> registerValidator,
        IValidator<LoginDto> loginValidator,
        IValidator<ForgotPasswordDto> forgotPasswordValidator,
        IValidator<ResetPasswordDto> resetPasswordValidator,
        IValidator<ChangePasswordDto> changePasswordValidator,
        IValidator<ConfirmEmailDto> confirmEmailValidator)
    {
        _authService = authService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _forgotPasswordValidator = forgotPasswordValidator;
        _resetPasswordValidator = resetPasswordValidator;
        _changePasswordValidator = changePasswordValidator;
        _confirmEmailValidator = confirmEmailValidator;
    }


    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(RegisterDto registerDto)
    {
        var validationResult = await _registerValidator.ValidateAsync(registerDto);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        try
        {
            var deviceInfo = Request.Headers["User-Agent"].ToString();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var result = await _authService.RegisterAsync(registerDto, deviceInfo, ipAddress);
            return CreatedAtAction(nameof(GetProfile), new { }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginDto loginDto)
    {
        var validationResult = await _loginValidator.ValidateAsync(loginDto);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        try
        {
            var deviceInfo = Request.Headers["User-Agent"].ToString();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var result = await _authService.LoginAsync(loginDto, deviceInfo, ipAddress);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(RefreshTokenDto refreshTokenDto)
    {
        try
        {
            var deviceInfo = Request.Headers["User-Agent"].ToString();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var result = await _authService.RefreshTokenAsync(refreshTokenDto, deviceInfo, ipAddress);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "Invalid refresh token" });
        }
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            await _authService.LogoutAsync(userId);
        }
        return Ok();
    }

    [Authorize]
    [HttpPost("revoke")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeToken([FromBody] string refreshToken)
    {
        try
        {
            await _authService.RevokeTokenAsync(refreshToken);
            return Ok();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Refresh token not found" });
        }
    }

    [Authorize]
    [HttpGet("profile")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        try
        {
            var result = await _authService.GetProfileAsync(userId);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
    
    [HttpPost("forgot-password")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
{
    var validationResult = await _forgotPasswordValidator.ValidateAsync(dto);
    if (!validationResult.IsValid)
        return BadRequest(validationResult.Errors);

    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    await _authService.ForgotPasswordAsync(dto, ipAddress);
    return Ok(new { message = "If the email exists, a reset link has been sent" });
}

[HttpPost("reset-password")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
{
    var validationResult = await _resetPasswordValidator.ValidateAsync(dto);
    if (!validationResult.IsValid)
        return BadRequest(validationResult.Errors);

    try
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        await _authService.ResetPasswordAsync(dto, ipAddress);
        return Ok(new { message = "Password has been reset successfully" });
    }
    catch (InvalidOperationException ex)
    {
        return BadRequest(new { message = ex.Message });
    }
}

[Authorize]
[HttpPost("change-password")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
{
    var validationResult = await _changePasswordValidator.ValidateAsync(dto);
    if (!validationResult.IsValid)
        return BadRequest(validationResult.Errors);

    var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (!Guid.TryParse(userIdClaim, out var userId))
        return Unauthorized();

    try
    {
        await _authService.ChangePasswordAsync(userId, dto);
        return Ok(new { message = "Password changed successfully" });
    }
    catch (UnauthorizedAccessException ex)
    {
        return Unauthorized(new { message = ex.Message });
    }
    catch (KeyNotFoundException ex)
    {
        return NotFound(new { message = ex.Message });
    }
}

[HttpPost("confirm-email")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailDto dto)
{
    var validationResult = await _confirmEmailValidator.ValidateAsync(dto);
    if (!validationResult.IsValid)
        return BadRequest(validationResult.Errors);

    try
    {
        await _authService.ConfirmEmailAsync(dto);
        return Ok(new { message = "Email confirmed successfully" });
    }
    catch (KeyNotFoundException)
    {
        return NotFound(new { message = "User not found" });
    }
    catch (InvalidOperationException ex)
    {
        return BadRequest(new { message = ex.Message });
    }
}
    
}