using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DocumentArchive.Core.DTOs.Auth;
using DocumentArchive.Core.Interfaces.Services;
using DocumentArchive.Core.Models;
using DocumentArchive.Services.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace DocumentArchive.Tests.ServicesTests;

public class AuthServiceTests : TestBase
{
    private readonly Mock<IAuditService> _auditMock;
    private readonly IConfiguration _config;
    private readonly AuthService _service;
    private readonly Mock<ITokenService> _tokenMock;

    public AuthServiceTests()
    {
        var mapper = TestHelper.CreateMapper();
        _tokenMock = new Mock<ITokenService>();
        _auditMock = new Mock<IAuditService>();

        var inMemorySettings = new Dictionary<string, string>
        {
            { "JwtSettings:SecretKey", "TestSecretKeyThatIsAtLeast32CharactersLong!!!" },
            { "JwtSettings:Issuer", "test" },
            { "JwtSettings:Audience", "test" }
        };
        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        _service = new AuthService(Context, _tokenMock.Object, mapper, NullLogger<AuthService>.Instance, _config,
            _auditMock.Object);
    }

    protected override void SeedData()
    {
        // Добавляем роль User
        Context.Roles.Add(new Role { Id = Guid.NewGuid(), Name = "User", Description = "Regular user" });
        Context.SaveChanges();
    }

    [Fact]
    public async Task RegisterAsync_ValidDto_ShouldCreateUser()
    {
        var dto = new RegisterDto
        {
            Email = "new@test.com",
            Username = "newuser",
            Password = "Pass123!",
            ConfirmPassword = "Pass123!",
            FirstName = "New",
            LastName = "User"
        };
        var result = await _service.RegisterAsync(dto, "device", "127.0.0.1");
        result.UserId.Should().NotBeEmpty();
        result.Email.Should().Be("new@test.com");
        result.Username.Should().Be("newuser");
        result.Roles.Should().Contain("User");
        _auditMock.Verify(x => x.LogAsync(
            SecurityEventType.Register,
            result.UserId,
            "new@test.com",
            "127.0.0.1",
            "device",
            true,
            It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ShouldThrowAndLogFail()
    {
        // создаём пользователя
        Context.Users.Add(new User
            { Id = Guid.NewGuid(), Email = "existing@test.com", Username = "existing", PasswordHash = "hash" });
        await Context.SaveChangesAsync();
        var dto = new RegisterDto
        {
            Email = "existing@test.com",
            Username = "new",
            Password = "Pass123!",
            ConfirmPassword = "Pass123!"
        };
        await FluentActions.Invoking(() => _service.RegisterAsync(dto, "device", "127.0.0.1"))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already registered*");
        _auditMock.Verify(x => x.LogAsync(
            SecurityEventType.Register,
            null,
            "existing@test.com",
            "127.0.0.1",
            "device",
            false,
            It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_CorrectCredentials_ShouldReturnTokens()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@test.com",
            Username = "test",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass123!"),
            IsActive = true,
            IsDeleted = false
        };
        Context.Users.Add(user);
        var role = await Context.Roles.FirstAsync();
        Context.UserRoles.Add(new UserRole { UserId = userId, RoleId = role.Id });
        await Context.SaveChangesAsync();

        _tokenMock.Setup(x =>
                x.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IList<string>>(), It.IsAny<IList<string>>()))
            .Returns("access_token");
        _tokenMock.Setup(x => x.GenerateRefreshTokenAsync(userId, "device", "127.0.0.1"))
            .ReturnsAsync("refresh_token");
        _tokenMock.Setup(x => x.GetAccessTokenExpiry()).Returns(DateTime.UtcNow.AddMinutes(15));
        _tokenMock.Setup(x => x.GetRefreshTokenExpiry()).Returns(DateTime.UtcNow.AddDays(7));

        var dto = new LoginDto { EmailOrUsername = "test@test.com", Password = "Pass123!" };
        var result = await _service.LoginAsync(dto, "device", "127.0.0.1");
        result.AccessToken.Should().Be("access_token");
        result.RefreshToken.Should().Be("refresh_token");
        result.UserId.Should().Be(userId);
        _auditMock.Verify(x => x.LogAsync(
            SecurityEventType.Login,
            userId,
            "test@test.com",
            "127.0.0.1",
            "device",
            true,
            It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ShouldThrowAndLog()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@test.com",
            Username = "test",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass123!"),
            IsActive = true
        };
        Context.Users.Add(user);
        await Context.SaveChangesAsync();
        var dto = new LoginDto { EmailOrUsername = "test@test.com", Password = "wrong" };
        await FluentActions.Invoking(() => _service.LoginAsync(dto, "device", "127.0.0.1"))
            .Should().ThrowAsync<UnauthorizedAccessException>();
        _auditMock.Verify(x => x.LogAsync(
            SecurityEventType.FailedLogin,
            user.Id,
            user.Email,
            "127.0.0.1",
            "device",
            false,
            It.IsAny<object>()), Times.Once);
    }
    

    [Fact]
    public async Task LogoutAsync_ShouldRevokeSessions()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@test.com" };
        Context.Users.Add(user);
        await Context.SaveChangesAsync();
        _tokenMock.Setup(x => x.RevokeAllUserSessionsAsync(userId)).Returns(Task.CompletedTask);
        await _service.LogoutAsync(userId, "127.0.0.1", "device");
        _tokenMock.Verify(x => x.RevokeAllUserSessionsAsync(userId), Times.Once);
        _auditMock.Verify(x => x.LogAsync(
            SecurityEventType.Logout,
            userId,
            user.Email,
            "127.0.0.1",
            "device",
            true,
            null), Times.Once);
    }

    [Fact]
    public async Task RevokeTokenAsync_ValidToken_ShouldRevoke()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@test.com" };
        Context.Users.Add(user);
        var refreshToken = "token";
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RefreshTokenHash = BCrypt.Net.BCrypt.HashPassword(refreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            User = user
        };
        Context.UserSessions.Add(session);
        await Context.SaveChangesAsync();
        await _service.RevokeTokenAsync(refreshToken, "127.0.0.1", "device");
        var updated = await Context.UserSessions.FindAsync(session.Id);
        updated!.IsRevoked.Should().BeTrue();
        _auditMock.Verify(x => x.LogAsync(
            SecurityEventType.TokenRevoke,
            userId,
            user.Email,
            "127.0.0.1",
            "device",
            true,
            It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task GetProfileAsync_ExistingUser_ShouldReturnProfile()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@test.com",
            Username = "test",
            FirstName = "Test",
            LastName = "User",
            IsActive = true
        };
        Context.Users.Add(user);
        var role = await Context.Roles.FirstAsync();
        Context.UserRoles.Add(new UserRole { UserId = userId, RoleId = role.Id });
        await Context.SaveChangesAsync();
        var result = await _service.GetProfileAsync(userId);
        result.UserId.Should().Be(userId);
        result.Roles.Should().Contain("User");
    }

    [Fact]
    public async Task ForgotPasswordAsync_ExistingUser_ShouldGenerateTokenAndLog()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "test@test.com", Username = "test" };
        Context.Users.Add(user);
        await Context.SaveChangesAsync();
        var dto = new ForgotPasswordDto { Email = "test@test.com" };
        await _service.ForgotPasswordAsync(dto, "127.0.0.1", "device");
        Context.PasswordResetTokens.Count().Should().Be(1);
        _auditMock.Verify(x => x.LogAsync(
            SecurityEventType.PasswordResetRequested,
            user.Id,
            user.Email,
            "127.0.0.1",
            "device",
            true,
            It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task ForgotPasswordAsync_NonExistingUser_ShouldLogFailWithoutException()
    {
        var dto = new ForgotPasswordDto { Email = "nonexistent@test.com" };
        await _service.ForgotPasswordAsync(dto, "127.0.0.1", "device");
        Context.PasswordResetTokens.Count().Should().Be(0);
        _auditMock.Verify(x => x.LogAsync(
            SecurityEventType.PasswordResetRequested,
            null,
            "nonexistent@test.com",
            "127.0.0.1",
            "device",
            false,
            It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAsync_ValidToken_ShouldChangePassword()
    {
        var user = new User
        {
            Id = Guid.NewGuid(), Email = "test@test.com", Username = "test",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("old")
        };
        Context.Users.Add(user);
        var tokenValue = "reset-token";
        var tokenEntity = new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = BCrypt.Net.BCrypt.HashPassword(tokenValue),
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = false,
            User = user
        };
        Context.PasswordResetTokens.Add(tokenEntity);
        await Context.SaveChangesAsync();
        var dto = new ResetPasswordDto
            { Token = tokenValue, NewPassword = "NewPass123!", ConfirmNewPassword = "NewPass123!" };
        await _service.ResetPasswordAsync(dto, "127.0.0.1", "device");
        var updatedUser = await Context.Users.FindAsync(user.Id);
        BCrypt.Net.BCrypt.Verify("NewPass123!", updatedUser!.PasswordHash).Should().BeTrue();
        tokenEntity.IsUsed.Should().BeTrue();
        _auditMock.Verify(x => x.LogAsync(
            SecurityEventType.PasswordReset,
            user.Id,
            user.Email,
            "127.0.0.1",
            "device",
            true,
            null), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_ValidCurrent_ShouldUpdate()
    {
        var user = new User
        {
            Id = Guid.NewGuid(), Email = "test@test.com", Username = "test",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("current")
        };
        Context.Users.Add(user);
        await Context.SaveChangesAsync();
        var dto = new ChangePasswordDto
            { CurrentPassword = "current", NewPassword = "newPass123!", ConfirmNewPassword = "newPass123!" };
        await _service.ChangePasswordAsync(user.Id, dto, "127.0.0.1", "device");
        var updated = await Context.Users.FindAsync(user.Id);
        BCrypt.Net.BCrypt.Verify("newPass123!", updated!.PasswordHash).Should().BeTrue();
        _auditMock.Verify(x => x.LogAsync(
            SecurityEventType.PasswordChange,
            user.Id,
            user.Email,
            "127.0.0.1",
            "device",
            true,
            null), Times.Once);
    }
}