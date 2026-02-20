using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DocumentArchive.Core.Models;
using DocumentArchive.Services.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DocumentArchive.Tests.ServicesTests;

public class TokenServiceTests : TestBase
{
    private readonly Mock<IConfiguration> _configMock;
    private readonly TokenService _service;

    public TokenServiceTests()
    {
        _configMock = new Mock<IConfiguration>();
        SetupConfig();
        _service = new TokenService(_configMock.Object, Context, NullLogger<TokenService>.Instance);
    }

    private void SetupConfig()
    {
        var sectionMock = new Mock<IConfigurationSection>();
        sectionMock.Setup(x => x["SecretKey"]).Returns("ThisIsASecretKeyThatIsLongEnoughForTesting123!");
        sectionMock.Setup(x => x["Issuer"]).Returns("test");
        sectionMock.Setup(x => x["Audience"]).Returns("test");
        sectionMock.Setup(x => x["AccessTokenExpirationMinutes"]).Returns("15");
        sectionMock.Setup(x => x["RefreshTokenExpirationDays"]).Returns("7");
        _configMock.Setup(x => x.GetSection("JwtSettings")).Returns(sectionMock.Object);
    }

    [Fact]
    public void GenerateAccessToken_ShouldIncludeAllClaims()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@test.com",
            Username = "testuser",
            FirstName = "Test",
            LastName = "User",
            DateOfBirth = new DateTime(1990, 1, 1)
        };
        var roles = new List<string> { "Admin", "User" };
        var permissions = new List<string> { "View", "Edit" };

        var token = _service.GenerateAccessToken(user, roles, permissions);
        token.Should().NotBeNullOrEmpty();

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id.ToString());
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.UniqueName && c.Value == user.Username);
        jwtToken.Claims.Should().Contain(c => c.Type == "firstName" && c.Value == user.FirstName);
        jwtToken.Claims.Should().Contain(c => c.Type == "lastName" && c.Value == user.LastName);
        jwtToken.Claims.Should().Contain(c => c.Type == "DateOfBirth" && c.Value == "1990-01-01");
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "User");
        jwtToken.Claims.Should().Contain(c => c.Type == "permission" && c.Value == "View");
        jwtToken.Claims.Should().Contain(c => c.Type == "permission" && c.Value == "Edit");
    }

    [Fact]
    public async Task GenerateRefreshTokenAsync_ShouldCreateSessionAndReturnToken()
    {
        var userId = Guid.NewGuid();
        var deviceInfo = "test-device";
        var ipAddress = "127.0.0.1";
        var token = await _service.GenerateRefreshTokenAsync(userId, deviceInfo, ipAddress);
        token.Should().NotBeNullOrEmpty();
        token.Length.Should().BeGreaterThan(20);

        var sessions = Context.UserSessions.ToList();
        sessions.Should().HaveCount(1);
        var session = sessions.First();
        session.UserId.Should().Be(userId);
        session.DeviceInfo.Should().Be(deviceInfo);
        session.IpAddress.Should().Be(ipAddress);
        session.IsRevoked.Should().BeFalse();
        session.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_ValidToken_ShouldReturnSession()
    {
        var userId = Guid.NewGuid();
        var token = await _service.GenerateRefreshTokenAsync(userId, "device", "127.0.0.1");
        var session = await _service.ValidateRefreshTokenAsync(userId, token);
        session.Should().NotBeNull();
        session!.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_InvalidToken_ShouldReturnNull()
    {
        var userId = Guid.NewGuid();
        var token = await _service.GenerateRefreshTokenAsync(userId, "device", "127.0.0.1");
        var result = await _service.ValidateRefreshTokenAsync(userId, "invalid-token");
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_ExpiredToken_ShouldReturnNull()
    {
        var userId = Guid.NewGuid();
        var token = await _service.GenerateRefreshTokenAsync(userId, "device", "127.0.0.1");
        var session = Context.UserSessions.First();
        session.ExpiresAt = DateTime.UtcNow.AddDays(-1);
        await Context.SaveChangesAsync();
        var result = await _service.ValidateRefreshTokenAsync(userId, token);
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_RevokedToken_ShouldReturnNull()
    {
        var userId = Guid.NewGuid();
        var token = await _service.GenerateRefreshTokenAsync(userId, "device", "127.0.0.1");
        var session = Context.UserSessions.First();
        session.IsRevoked = true;
        await Context.SaveChangesAsync();
        var result = await _service.ValidateRefreshTokenAsync(userId, token);
        result.Should().BeNull();
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_ShouldMarkSessionAsRevoked()
    {
        var userId = Guid.NewGuid();
        var token = await _service.GenerateRefreshTokenAsync(userId, "device", "127.0.0.1");
        var session = Context.UserSessions.First();
        await _service.RevokeRefreshTokenAsync(session.Id);
        var updated = await Context.UserSessions.FindAsync(session.Id);
        updated!.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeAllUserSessionsAsync_ShouldRevokeAll()
    {
        var userId = Guid.NewGuid();
        await _service.GenerateRefreshTokenAsync(userId, "device1", "127.0.0.1");
        await _service.GenerateRefreshTokenAsync(userId, "device2", "127.0.0.1");
        await _service.RevokeAllUserSessionsAsync(userId);
        Context.UserSessions.Count(s => !s.IsRevoked).Should().Be(0);
    }

    [Fact]
    public void GetAccessTokenExpiry_ShouldReturnFutureTime()
    {
        var expiry = _service.GetAccessTokenExpiry();
        expiry.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(15), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GetRefreshTokenExpiry_ShouldReturnFutureTime()
    {
        var expiry = _service.GetRefreshTokenExpiry();
        expiry.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromSeconds(5));
    }
}