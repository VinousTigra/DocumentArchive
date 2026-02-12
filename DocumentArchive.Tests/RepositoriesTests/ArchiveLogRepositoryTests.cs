using DocumentArchive.Core.Models;
using DocumentArchive.Infrastructure.Repositories;
using FluentAssertions;

namespace DocumentArchive.Tests.RepositoriesTests;

public class ArchiveLogRepositoryTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly ArchiveLogRepository _repository;

    public ArchiveLogRepositoryTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
        Environment.CurrentDirectory = _testDirectory;
        _repository = new ArchiveLogRepository();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
            Directory.Delete(_testDirectory, true);
    }

    [Fact]
    public async Task AddAsync_ShouldAddLog()
    {
        // Arrange
        var log = new ArchiveLog
        {
            Id = Guid.NewGuid(),
            Action = "Created",
            ActionType = ActionType.Created,
            Timestamp = DateTime.UtcNow,
            DocumentId = Guid.NewGuid(),
            UserId = Guid.NewGuid()
        };

        // Act
        await _repository.AddAsync(log);
        var retrieved = await _repository.GetByIdAsync(log.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Action.Should().Be("Created");
    }

    [Fact]
    public async Task GetByDocumentIdAsync_ShouldReturnLogsForDocument()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var log1 = new ArchiveLog { Id = Guid.NewGuid(), DocumentId = docId, Action = "Created" };
        var log2 = new ArchiveLog { Id = Guid.NewGuid(), DocumentId = docId, Action = "Updated" };
        var log3 = new ArchiveLog { Id = Guid.NewGuid(), DocumentId = Guid.NewGuid(), Action = "Deleted" };
        await _repository.AddAsync(log1);
        await _repository.AddAsync(log2);
        await _repository.AddAsync(log3);

        // Act
        var result = await _repository.GetByDocumentIdAsync(docId);

        // Assert
        result.Should().HaveCount(2);
        result.Select(l => l.Action).Should().Contain(new[] { "Created", "Updated" });
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnLogsForUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var log1 = new ArchiveLog { Id = Guid.NewGuid(), UserId = userId, Action = "Created" };
        var log2 = new ArchiveLog { Id = Guid.NewGuid(), UserId = userId, Action = "Updated" };
        await _repository.AddAsync(log1);
        await _repository.AddAsync(log2);

        // Act
        var result = await _repository.GetByUserIdAsync(userId);

        // Assert
        result.Should().HaveCount(2);
    }
}