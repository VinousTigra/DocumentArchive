using System.Security.Claims;
using DocumentArchive.API.Controllers;
using DocumentArchive.Core.DTOs.Auth;
using DocumentArchive.Core.Interfaces.Services;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DocumentArchive.Tests.ControllersTests;

public class AuthControllerTests
{
    private readonly AuthController _controller;
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<IValidator<RegisterDto>> _registerValidatorMock;
    private readonly Mock<IValidator<LoginDto>> _loginValidatorMock;
    private readonly Mock<IValidator<ForgotPasswordDto>> _forgotPasswordValidatorMock;
    private readonly Mock<IValidator<ResetPasswordDto>> _resetPasswordValidatorMock;
    private readonly Mock<IValidator<ChangePasswordDto>> _changePasswordValidatorMock;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _registerValidatorMock = new Mock<IValidator<RegisterDto>>();
        _loginValidatorMock = new Mock<IValidator<LoginDto>>();
        _forgotPasswordValidatorMock = new Mock<IValidator<ForgotPasswordDto>>();
        _resetPasswordValidatorMock = new Mock<IValidator<ResetPasswordDto>>();
        _changePasswordValidatorMock = new Mock<IValidator<ChangePasswordDto>>();

        _controller = new AuthController(
            _authServiceMock.Object,
            _registerValidatorMock.Object,
            _loginValidatorMock.Object,
            _forgotPasswordValidatorMock.Object,
            _resetPasswordValidatorMock.Object,
            _changePasswordValidatorMock.Object);

        // Устанавливаем минимальный HttpContext для всех тестов
        SetupHttpContext();
    }

    private void SetupHttpContext()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    private void SetupUser(Guid userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    [Fact]
    public async Task Register_ValidDto_ShouldReturnCreated()
    {
        // Arrange
        var dto = new RegisterDto { Email = "test@test.com", Username = "test", Password = "Pass123!", ConfirmPassword = "Pass123!" };
        var response = new AuthResponseDto { UserId = Guid.NewGuid() };
        _registerValidatorMock.Setup(x => x.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _authServiceMock.Setup(x => x.RegisterAsync(dto, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.Register(dto);

        // Assert
        var createdResult = result as CreatedAtActionResult;
        createdResult.Should().NotBeNull();
        createdResult!.StatusCode.Should().Be(201);
        createdResult.ActionName.Should().Be(nameof(_controller.GetProfile));
        createdResult.Value.Should().Be(response);
    }

    [Fact]
    public async Task Register_InvalidDto_ShouldReturnBadRequest()
    {
        // Arrange
        var dto = new RegisterDto { Email = "invalid" };
        var failures = new List<ValidationFailure> { new("Email", "Invalid email") };
        _registerValidatorMock.Setup(x => x.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        // Act
        var result = await _controller.Register(dto);

        // Assert
        var badRequest = result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.StatusCode.Should().Be(400);
        var errors = badRequest.Value as IEnumerable<ValidationFailure>;
        errors.Should().NotBeNull();
        errors!.Select(e => e.ErrorMessage).Should().Contain("Invalid email");
    }

    [Fact]
    public async Task Register_ServiceThrowsInvalidOperation_ShouldReturnBadRequest()
    {
        // Arrange
        var dto = new RegisterDto { Email = "test@test.com", Username = "test", Password = "Pass123!", ConfirmPassword = "Pass123!" };
        _registerValidatorMock.Setup(x => x.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _authServiceMock.Setup(x => x.RegisterAsync(dto, It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Email already exists"));

        // Act
        var result = await _controller.Register(dto);

        // Assert
        var badRequest = result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.StatusCode.Should().Be(400);
        var value = badRequest.Value;
        var message = value?.GetType().GetProperty("message")?.GetValue(value)?.ToString();
        message.Should().Be("Email already exists");
    }

    [Fact]
    public async Task Login_ValidCredentials_ShouldReturnOk()
    {
        // Arrange
        var dto = new LoginDto { EmailOrUsername = "test", Password = "pass" };
        var response = new AuthResponseDto { AccessToken = "token" };
        _loginValidatorMock.Setup(x => x.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _authServiceMock.Setup(x => x.LoginAsync(dto, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.Login(dto);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(response);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ShouldReturnUnauthorized()
    {
        // Arrange
        var dto = new LoginDto { EmailOrUsername = "test", Password = "wrong" };
        _loginValidatorMock.Setup(x => x.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _authServiceMock.Setup(x => x.LoginAsync(dto, It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new UnauthorizedAccessException());

        // Act
        var result = await _controller.Login(dto);

        // Assert
        var unauthorized = result as UnauthorizedObjectResult;
        unauthorized.Should().NotBeNull();
        unauthorized!.StatusCode.Should().Be(401);
        var value = unauthorized.Value;
        var message = value?.GetType().GetProperty("message")?.GetValue(value)?.ToString();
        message.Should().Be("Invalid credentials");
    }

    [Fact]
    public async Task Refresh_ValidToken_ShouldReturnOk()
    {
        // Arrange
        var dto = new RefreshTokenDto { AccessToken = "old", RefreshToken = "refresh" };
        var response = new AuthResponseDto { AccessToken = "new" };
        _authServiceMock.Setup(x => x.RefreshTokenAsync(dto, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.Refresh(dto);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(response);
    }

    [Fact]
    public async Task Refresh_InvalidToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var dto = new RefreshTokenDto { AccessToken = "old", RefreshToken = "invalid" };
        _authServiceMock.Setup(x => x.RefreshTokenAsync(dto, It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new UnauthorizedAccessException());

        // Act
        var result = await _controller.Refresh(dto);

        // Assert
        var unauthorized = result as UnauthorizedObjectResult;
        unauthorized.Should().NotBeNull();
        unauthorized!.StatusCode.Should().Be(401);
        var value = unauthorized.Value;
        var message = value?.GetType().GetProperty("message")?.GetValue(value)?.ToString();
        message.Should().Be("Invalid refresh token");
    }

    [Fact]
    public async Task Logout_ShouldReturnOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupUser(userId);
        _authServiceMock.Setup(x => x.LogoutAsync(userId, It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Logout();

        // Assert
        var okResult = result as OkResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task RevokeToken_ValidToken_ShouldReturnOk()
    {
        // Arrange
        SetupUser(Guid.NewGuid()); // для User не нужен, но контроллер его не использует
        var refreshToken = "token";
        _authServiceMock.Setup(x => x.RevokeTokenAsync(refreshToken, It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.RevokeToken(refreshToken);

        // Assert
        var okResult = result as OkResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task RevokeToken_TokenNotFound_ShouldReturnNotFound()
    {
        // Arrange
        SetupUser(Guid.NewGuid());
        var refreshToken = "invalid";
        _authServiceMock.Setup(x => x.RevokeTokenAsync(refreshToken, It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new KeyNotFoundException());

        // Act
        var result = await _controller.RevokeToken(refreshToken);

        // Assert
        var notFound = result as NotFoundObjectResult;
        notFound.Should().NotBeNull();
        notFound!.StatusCode.Should().Be(404);
        var value = notFound.Value;
        var message = value?.GetType().GetProperty("message")?.GetValue(value)?.ToString();
        message.Should().Be("Refresh token not found");
    }

    [Fact]
    public async Task GetProfile_ShouldReturnOk_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupUser(userId);
        var profile = new AuthResponseDto { UserId = userId, Username = "test" };
        _authServiceMock.Setup(x => x.GetProfileAsync(userId))
            .ReturnsAsync(profile);

        // Act
        var result = await _controller.GetProfile();


    }

    [Fact]
    public async Task GetProfile_UserNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupUser(userId);
        _authServiceMock.Setup(x => x.GetProfileAsync(userId))
            .ThrowsAsync(new KeyNotFoundException());

        // Act
        var result = await _controller.GetProfile();

    }

    [Fact]
    public async Task ForgotPassword_ValidEmail_ShouldReturnOk()
    {
        // Arrange
        var dto = new ForgotPasswordDto { Email = "test@test.com" };
        _forgotPasswordValidatorMock.Setup(x => x.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _authServiceMock.Setup(x => x.ForgotPasswordAsync(dto, It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ForgotPassword(dto);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        var value = okResult.Value;
        var message = value?.GetType().GetProperty("message")?.GetValue(value)?.ToString();
        message.Should().Be("If the email exists, a reset link has been sent");
    }

    [Fact]
    public async Task ForgotPassword_InvalidEmail_ShouldReturnBadRequest()
    {
        // Arrange
        var dto = new ForgotPasswordDto { Email = "" };
        var failures = new List<ValidationFailure> { new("Email", "Email required") };
        _forgotPasswordValidatorMock.Setup(x => x.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        // Act
        var result = await _controller.ForgotPassword(dto);

        // Assert
        var badRequest = result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.StatusCode.Should().Be(400);
        var errors = badRequest.Value as IEnumerable<ValidationFailure>;
        errors.Should().NotBeNull();
        errors!.Select(e => e.ErrorMessage).Should().Contain("Email required");
    }

    [Fact]
    public async Task ResetPassword_ValidToken_ShouldReturnOk()
    {
        // Arrange
        var dto = new ResetPasswordDto { Token = "token", NewPassword = "NewPass123!", ConfirmNewPassword = "NewPass123!" };
        _resetPasswordValidatorMock.Setup(x => x.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _authServiceMock.Setup(x => x.ResetPasswordAsync(dto, It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ResetPassword(dto);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        var value = okResult.Value;
        var message = value?.GetType().GetProperty("message")?.GetValue(value)?.ToString();
        message.Should().Be("Password has been reset successfully");
    }

    [Fact]
    public async Task ResetPassword_InvalidToken_ShouldReturnBadRequest()
    {
        // Arrange
        var dto = new ResetPasswordDto { Token = "invalid", NewPassword = "NewPass123!", ConfirmNewPassword = "NewPass123!" };
        _resetPasswordValidatorMock.Setup(x => x.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _authServiceMock.Setup(x => x.ResetPasswordAsync(dto, It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Invalid token"));

        // Act
        var result = await _controller.ResetPassword(dto);

        // Assert
        var badRequest = result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.StatusCode.Should().Be(400);
        var value = badRequest.Value;
        var message = value?.GetType().GetProperty("message")?.GetValue(value)?.ToString();
        message.Should().Be("Invalid token");
    }

    [Fact]
    public async Task ChangePassword_Valid_ShouldReturnOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupUser(userId);
        var dto = new ChangePasswordDto { CurrentPassword = "old", NewPassword = "NewPass123!", ConfirmNewPassword = "NewPass123!" };
        _changePasswordValidatorMock.Setup(x => x.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _authServiceMock.Setup(x => x.ChangePasswordAsync(userId, dto, It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ChangePassword(dto);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        var value = okResult.Value;
        var message = value?.GetType().GetProperty("message")?.GetValue(value)?.ToString();
        message.Should().Be("Password changed successfully");
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_ShouldReturnUnauthorized()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupUser(userId);
        var dto = new ChangePasswordDto { CurrentPassword = "wrong", NewPassword = "NewPass123!", ConfirmNewPassword = "NewPass123!" };
        _changePasswordValidatorMock.Setup(x => x.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _authServiceMock.Setup(x => x.ChangePasswordAsync(userId, dto, It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new UnauthorizedAccessException("Current password is incorrect"));

        // Act
        var result = await _controller.ChangePassword(dto);

        // Assert
        var unauthorized = result as UnauthorizedObjectResult;
        unauthorized.Should().NotBeNull();
        unauthorized!.StatusCode.Should().Be(401);
        var value = unauthorized.Value;
        var message = value?.GetType().GetProperty("message")?.GetValue(value)?.ToString();
        message.Should().Be("Current password is incorrect");
    }
}