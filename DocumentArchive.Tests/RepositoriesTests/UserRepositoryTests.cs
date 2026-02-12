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
        Environment.CurrentDirectory = _testDirectory;
        _repository = new UserRepository();
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
            Email = "test@example.com"
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
    public async Task FindByEmailAsync_ShouldReturnUser_WhenExists()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "john",
            Email = "john@example.com"
        };
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
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "alice",
            Email = "alice@example.com"
        };
        await _repository.AddAsync(user);

        // Act
        var result = await _repository.FindByUsernameAsync("alice");

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("alice@example.com");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveUser()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Username = "temp", Email = "temp@ex.com" };
        await _repository.AddAsync(user);

        // Act
        await _repository.DeleteAsync(user.Id);
        var result = await _repository.GetByIdAsync(user.Id);

        // Assert
        result.Should().BeNull();
    }
}