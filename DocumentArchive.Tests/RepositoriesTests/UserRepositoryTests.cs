using DocumentArchive.Core.Models;
using DocumentArchive.Infrastructure.Repositories;
using FluentAssertions;

namespace DocumentArchive.Tests.RepositoriesTests;

public class UserRepositoryTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly UserRepository _repository;

    public UserRepositoryTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
        _repository = new UserRepository(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
            Directory.Delete(_testDirectory, true);
    }

    [Fact]
    public async Task AddAsync_ShouldAddUser()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await _repository.AddAsync(user);
        var retrieved = await _repository.GetByIdAsync(user.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Username.Should().Be("testuser");
        retrieved.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllUsers()
    {
        // Arrange
        var user1 = new User { Id = Guid.NewGuid(), Username = "user1", Email = "user1@test.com" };
        var user2 = new User { Id = Guid.NewGuid(), Username = "user2", Email = "user2@test.com" };
        await _repository.AddAsync(user1);
        await _repository.AddAsync(user2);

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Select(u => u.Username).Should().Contain(new[] { "user1", "user2" });
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnUser_WhenExists()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Username = "test", Email = "test@test.com" };
        await _repository.AddAsync(user);

        // Act
        var result = await _repository.GetByIdAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Username.Should().Be("test");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindByEmailAsync_ShouldReturnUser_WhenExists()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Username = "john", Email = "john@example.com" };
        await _repository.AddAsync(user);

        // Act
        var result = await _repository.FindByEmailAsync("john@example.com");

        // Assert
        result.Should().NotBeNull();
        result!.Username.Should().Be("john");
    }

    [Fact]
    public async Task FindByEmailAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = await _repository.FindByEmailAsync("none@example.com");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindByUsernameAsync_ShouldReturnUser_WhenExists()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Username = "alice", Email = "alice@example.com" };
        await _repository.AddAsync(user);

        // Act
        var result = await _repository.FindByUsernameAsync("alice");

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("alice@example.com");
    }

    [Fact]
    public async Task FindByUsernameAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = await _repository.FindByUsernameAsync("bob");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyUser()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "oldname",
            Email = "old@test.com"
        };
        await _repository.AddAsync(user);

        user.Username = "newname";
        user.Email = "new@test.com";
        user.UpdatedAt = DateTime.UtcNow;

        // Act
        await _repository.UpdateAsync(user);
        var updated = await _repository.GetByIdAsync(user.Id);

        // Assert
        updated!.Username.Should().Be("newname");
        updated.Email.Should().Be("new@test.com");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveUser()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Username = "temp", Email = "temp@test.com" };
        await _repository.AddAsync(user);

        // Act
        await _repository.DeleteAsync(user.Id);
        var result = await _repository.GetByIdAsync(user.Id);

        // Assert
        result.Should().BeNull();
    }
}