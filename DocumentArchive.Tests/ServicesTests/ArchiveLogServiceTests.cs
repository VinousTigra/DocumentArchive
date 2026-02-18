using AutoMapper;
using DocumentArchive.Core.DTOs.ArchiveLog;
using DocumentArchive.Core.Models;
using DocumentArchive.Services.Mapping;
using DocumentArchive.Services.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace DocumentArchive.Tests.ServicesTests;

public class ArchiveLogServiceTests : TestBase
{
    private readonly ArchiveLogService _service;

    public ArchiveLogServiceTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        var mapper = config.CreateMapper();
        _service = new ArchiveLogService(Context, mapper, NullLogger<ArchiveLogService>.Instance);
    }

    [Fact]
    public async Task CreateLogAsync_ShouldAddLog()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Username = "u", Email = "u@t.com" };
        var doc = new Document { Id = Guid.NewGuid(), Title = "Doc" };
        Context.Users.Add(user);
        Context.Documents.Add(doc);
        await Context.SaveChangesAsync();

        var dto = new CreateArchiveLogDto
        {
            Action = "Viewed",
            ActionType = ActionType.Viewed,
            IsCritical = false,
            UserId = user.Id,
            DocumentId = doc.Id
        };

        // Act
        var result = await _service.CreateLogAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Action.Should().Be("Viewed");
        result.ActionType.Should().Be(ActionType.Viewed);

        var log = await Context.ArchiveLogs.FirstOrDefaultAsync(l => l.DocumentId == doc.Id);
        log.Should().NotBeNull();
        log.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task CreateLogAsync_ShouldThrow_WhenDocumentNotFound()
    {
        var dto = new CreateArchiveLogDto
        {
            DocumentId = Guid.NewGuid(),
            UserId = Guid.NewGuid()
        };
        Func<Task> act = async () => await _service.CreateLogAsync(dto);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Document with id {dto.DocumentId} not found");
    }

    [Fact]
    public async Task GetLogByIdAsync_ShouldReturnDto()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Username = "u", Email = "u@t.com" };
        var doc = new Document { Id = Guid.NewGuid(), Title = "Doc" };
        var log = new ArchiveLog
        {
            Id = Guid.NewGuid(),
            Action = "Created",
            ActionType = ActionType.Created,
            Timestamp = DateTime.UtcNow,
            UserId = user.Id,
            DocumentId = doc.Id,
            User = user,
            Document = doc
        };
        Context.Users.Add(user);
        Context.Documents.Add(doc);
        Context.ArchiveLogs.Add(log);
        await Context.SaveChangesAsync();

        // Act
        var result = await _service.GetLogByIdAsync(log.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(log.Id);
        result.Action.Should().Be("Created");
        result.UserName.Should().Be("u");
        result.DocumentTitle.Should().Be("Doc");
    }

    [Fact]
    public async Task DeleteLogAsync_ShouldRemoveLog()
    {
        // Arrange
        var log = new ArchiveLog { Id = Guid.NewGuid(), Action = "Test" };
        Context.ArchiveLogs.Add(log);
        await Context.SaveChangesAsync();

        // Act
        await _service.DeleteLogAsync(log.Id);

        // Assert
        var deleted = await Context.ArchiveLogs.FindAsync(log.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task GetLogsAsync_ShouldReturnPagedFiltered()
    {
        // Arrange
        var user1 = new User { Id = Guid.NewGuid(), Username = "u1" };
        var user2 = new User { Id = Guid.NewGuid(), Username = "u2" };
        var doc = new Document { Id = Guid.NewGuid(), Title = "Doc" };
        Context.Users.AddRange(user1, user2);
        Context.Documents.Add(doc);
        await Context.SaveChangesAsync();

        var logs = new List<ArchiveLog>
        {
            new() { Action = "Create", ActionType = ActionType.Created, UserId = user1.Id, DocumentId = doc.Id, Timestamp = DateTime.UtcNow.AddDays(-3) },
            new() { Action = "Update", ActionType = ActionType.Updated, UserId = user1.Id, DocumentId = doc.Id, Timestamp = DateTime.UtcNow.AddDays(-2), IsCritical = true },
            new() { Action = "Delete", ActionType = ActionType.Deleted, UserId = user2.Id, DocumentId = doc.Id, Timestamp = DateTime.UtcNow.AddDays(-1), IsCritical = false }
        };
        Context.ArchiveLogs.AddRange(logs);
        await Context.SaveChangesAsync();

        // Act: пагинация
        var page1 = await _service.GetLogsAsync(1, 2, null, null, null, null, null, null);
        page1.Items.Should().HaveCount(2);
        page1.TotalCount.Should().Be(3);

        // Фильтр по пользователю
        var userLogs = await _service.GetLogsAsync(1, 10, null, user1.Id, null, null, null, null);
        userLogs.Items.Should().HaveCount(2);

        // Фильтр по типу действия
        var typeLogs = await _service.GetLogsAsync(1, 10, null, null, null, null, ActionType.Created, null);
        typeLogs.Items.Should().HaveCount(1);
        typeLogs.Items.First().ActionType.Should().Be(ActionType.Created);

        // Фильтр по дате
        var from = DateTime.UtcNow.AddDays(-2);
        var to = DateTime.UtcNow;
        var dateLogs = await _service.GetLogsAsync(1, 10, null, null, from, to, null, null);
        dateLogs.Items.Should().HaveCount(2); // update и delete

        // Фильтр по критичности
        var critical = await _service.GetLogsAsync(1, 10, null, null, null, null, null, true);
        critical.Items.Should().HaveCount(1);
        critical.Items.First().Action.Should().Be("Update");
    }

    [Fact]
    public async Task GetLogsCountByActionTypeAsync_ShouldReturnCounts()
    {
        // Arrange
        var logs = new List<ArchiveLog>
        {
            new() { ActionType = ActionType.Created },
            new() { ActionType = ActionType.Created },
            new() { ActionType = ActionType.Updated },
            new() { ActionType = ActionType.Deleted }
        };
        Context.ArchiveLogs.AddRange(logs);
        await Context.SaveChangesAsync();

        // Act
        var result = await _service.GetLogsCountByActionTypeAsync();

        // Assert
        result[ActionType.Created].Should().Be(2);
        result[ActionType.Updated].Should().Be(1);
        result[ActionType.Deleted].Should().Be(1);
        result.Should().NotContainKey(ActionType.Viewed);
    }

    [Fact]
    public async Task GetLogsStatisticsAsync_ShouldReturnSummary()
    {
        // Arrange
        var logs = new List<ArchiveLog>
        {
            new() { ActionType = ActionType.Created, IsCritical = false },
            new() { ActionType = ActionType.Created, IsCritical = false },
            new() { ActionType = ActionType.Updated, IsCritical = true },
            new() { ActionType = ActionType.Deleted, IsCritical = false }
        };
        Context.ArchiveLogs.AddRange(logs);
        await Context.SaveChangesAsync();

        // Act
        var stats = await _service.GetLogsStatisticsAsync();

        // Assert
        stats.TotalLogs.Should().Be(4);
        stats.CriticalLogs.Should().Be(1);
        stats.LogsByActionType.Should().HaveCount(3);
        stats.LogsByActionType.First(l => l.ActionType == ActionType.Created).Count.Should().Be(2);
    }
}