using DocumentArchive.Core.Models;
using DocumentArchive.Infrastructure.Repositories;
using FluentAssertions;

namespace DocumentArchive.Tests.RepositoriesTests;

public class ArchiveLogRepositoryTests : IDisposable
{
    private readonly ArchiveLogRepository _repository;
    private readonly string _testDirectory;

    public ArchiveLogRepositoryTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
        _repository = new ArchiveLogRepository(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
            Directory.Delete(_testDirectory, true);
    }

    [Fact]
    public async Task AddAsync_ShouldAddLog()
    {
        var log = new ArchiveLog
        {
            Id = Guid.NewGuid(),
            Action = "Created",
            ActionType = ActionType.Created,
            Timestamp = DateTime.UtcNow,
            UserId = Guid.NewGuid(),
            DocumentId = Guid.NewGuid()
        };
        await _repository.AddAsync(log);
        var retrieved = await _repository.GetByIdAsync(log.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Action.Should().Be("Created");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllLogs()
    {
        var log1 = new ArchiveLog { Id = Guid.NewGuid(), Action = "Created" };
        var log2 = new ArchiveLog { Id = Guid.NewGuid(), Action = "Updated" };
        await _repository.AddAsync(log1);
        await _repository.AddAsync(log2);
        var result = await _repository.GetAllAsync();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByDocumentIdAsync_ShouldReturnLogsForDocument()
    {
        var docId = Guid.NewGuid();
        var log1 = new ArchiveLog { Id = Guid.NewGuid(), DocumentId = docId, Action = "Created" };
        var log2 = new ArchiveLog { Id = Guid.NewGuid(), DocumentId = docId, Action = "Updated" };
        var log3 = new ArchiveLog { Id = Guid.NewGuid(), DocumentId = Guid.NewGuid(), Action = "Deleted" };
        await _repository.AddAsync(log1);
        await _repository.AddAsync(log2);
        await _repository.AddAsync(log3);

        var result = await _repository.GetByDocumentIdAsync(docId);
        result.Should().HaveCount(2);
        result.Select(l => l.Action).Should().Contain(new[] { "Created", "Updated" });
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnLogsForUser()
    {
        var userId = Guid.NewGuid();
        var log1 = new ArchiveLog { Id = Guid.NewGuid(), UserId = userId, Action = "Created" };
        var log2 = new ArchiveLog { Id = Guid.NewGuid(), UserId = userId, Action = "Updated" };
        await _repository.AddAsync(log1);
        await _repository.AddAsync(log2);

        var result = await _repository.GetByUserIdAsync(userId);
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPagedAsync_ShouldApplyFiltersAndPagination()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var docId = Guid.NewGuid();
        var baseTime = DateTime.UtcNow;

        var logs = new List<ArchiveLog>();
        for (var i = 0; i < 20; i++)
            logs.Add(new ArchiveLog
            {
                Id = Guid.NewGuid(),
                Action = $"Action{i}",
                ActionType = i % 2 == 0 ? ActionType.Created : ActionType.Updated,
                IsCritical = i % 3 == 0,
                Timestamp = baseTime.AddMinutes(-i),
                UserId = i < 10 ? userId1 : userId2,
                DocumentId = i < 15 ? docId : Guid.NewGuid()
            });
        foreach (var log in logs)
            await _repository.AddAsync(log);

        // Act
        var result = await _repository.GetPagedAsync(
            2,
            5,
            docId,
            userId1,
            baseTime.AddHours(-1),
            baseTime.AddHours(1),
            ActionType.Created,
            null);

        // Assert
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(5);
        // Подсчёт ожидаемого количества: documentId = docId AND userId = userId1 AND ActionType = Created
        var expectedCount = logs.Count(l =>
            l.DocumentId == docId &&
            l.UserId == userId1 &&
            l.ActionType == ActionType.Created &&
            l.Timestamp >= baseTime.AddHours(-1) &&
            l.Timestamp <= baseTime.AddHours(1));
        result.TotalCount.Should().Be(expectedCount);
        result.Items.Should().HaveCount(Math.Min(5, expectedCount - 5));
        // Проверка сортировки (по убыванию Timestamp)
        var expectedItems = logs.Where(l =>
                l.DocumentId == docId &&
                l.UserId == userId1 &&
                l.ActionType == ActionType.Created &&
                l.Timestamp >= baseTime.AddHours(-1) &&
                l.Timestamp <= baseTime.AddHours(1))
            .OrderByDescending(l => l.Timestamp)
            .Skip(5)
            .Take(5)
            .ToList();
        result.Items.Select(l => l.Id).Should().Equal(expectedItems.Select(l => l.Id));
    }
}