using DocumentArchive.Core.Models;
using DocumentArchive.Infrastructure.Data;
using DocumentArchive.Services.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DocumentArchive.Tests.ServicesTests;

public class AuditServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly AuditService _service;

    public AuditServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _service = new AuditService(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task LogAsync_ShouldAddAuditLog()
    {
        // Arrange
        var eventType = SecurityEventType.Login;
        var userId = Guid.NewGuid();
        var userEmail = "test@test.com";
        var ip = "127.0.0.1";
        var userAgent = "Mozilla";
        var success = true;
        var details = new { Extra = "data" };

        // Act
        await _service.LogAsync(eventType, userId, userEmail, ip, userAgent, success, details);

        // Assert
        var logs = await _context.SecurityAuditLogs.ToListAsync();
        logs.Should().HaveCount(1);
        var log = logs.First();
        log.EventType.Should().Be(eventType);
        log.UserId.Should().Be(userId);
        log.UserEmail.Should().Be(userEmail);
        log.IpAddress.Should().Be(ip);
        log.UserAgent.Should().Be(userAgent);
        log.Success.Should().BeTrue();
        log.Details.Should().Contain("Extra");
    }
}