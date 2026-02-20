using AutoMapper;
using DocumentArchive.Core.DTOs.ArchiveLog;
using DocumentArchive.Core.Models;
using DocumentArchive.Infrastructure.Data;
using DocumentArchive.Services.Mapping;
using DocumentArchive.Services.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace DocumentArchive.Tests.ServicesTests;

public class ArchiveLogServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ArchiveLogService _service;

    public ArchiveLogServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        var mapper = config.CreateMapper();

        var loggerMock = new Mock<ILogger<ArchiveLogService>>();
        _service = new ArchiveLogService(_context, mapper, loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetLogsAsync_ShouldFilter()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var docId = Guid.NewGuid();
        var logs = new[]
        {
            new ArchiveLog
            {
                Id = Guid.NewGuid(), UserId = userId, DocumentId = docId, ActionType = ActionType.Created,
                Timestamp = DateTime.UtcNow
            },
            new ArchiveLog
            {
                Id = Guid.NewGuid(), UserId = userId, DocumentId = docId, ActionType = ActionType.Updated,
                Timestamp = DateTime.UtcNow
            },
            new ArchiveLog
            {
                Id = Guid.NewGuid(), UserId = Guid.NewGuid(), DocumentId = docId, ActionType = ActionType.Deleted,
                Timestamp = DateTime.UtcNow
            }
        };
        _context.ArchiveLogs.AddRange(logs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetLogsAsync(1, 10, docId, userId, null, null, null, null, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateLogAsync_ShouldCreate()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Username = "u" };
        var doc = new Document { Id = Guid.NewGuid(), Title = "Doc" };
        _context.Users.Add(user);
        _context.Documents.Add(doc);
        await _context.SaveChangesAsync();

        var dto = new CreateArchiveLogDto
        {
            Action = "Test",
            ActionType = ActionType.Created,
            DocumentId = doc.Id,
            UserId = user.Id,
            IsCritical = false
        };

        // Act
        var result = await _service.CreateLogAsync(dto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Action.Should().Be("Test");
    }

    [Fact]
    public async Task GetLogsCountByActionTypeAsync_ShouldReturnCounts()
    {
        // Arrange
        _context.ArchiveLogs.AddRange(
            new ArchiveLog { Id = Guid.NewGuid(), ActionType = ActionType.Created },
            new ArchiveLog { Id = Guid.NewGuid(), ActionType = ActionType.Created },
            new ArchiveLog { Id = Guid.NewGuid(), ActionType = ActionType.Updated }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetLogsCountByActionTypeAsync(null, null, CancellationToken.None);

        // Assert
        result[ActionType.Created].Should().Be(2);
        result[ActionType.Updated].Should().Be(1);
    }
}