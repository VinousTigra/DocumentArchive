using DocumentArchive.Core.Models;
using DocumentArchive.Infrastructure.Repositories;
using FluentAssertions;

namespace DocumentArchive.Tests.RepositoriesTests;

public class UserRepositoryTests : IDisposable
{
    private readonly UserRepository _repository;
    private readonly string _testDirectory;

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
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
            CreatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(user);
        var retrieved = await _repository.GetByIdAsync(user.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Username.Should().Be("testuser");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllUsers()
    {
        var user1 = new User { Id = Guid.NewGuid(), Username = "user1", Email = "u1@test.com" };
        var user2 = new User { Id = Guid.NewGuid(), Username = "user2", Email = "u2@test.com" };
        await _repository.AddAsync(user1);
        await _repository.AddAsync(user2);
        var result = await _repository.GetAllAsync();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyUser()
    {
        var user = new User { Id = Guid.NewGuid(), Username = "old", Email = "old@test.com" };
        await _repository.AddAsync(user);
        user.Username = "new";
        await _repository.UpdateAsync(user);
        var updated = await _repository.GetByIdAsync(user.Id);
        updated!.Username.Should().Be("new");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveUser()
    {
        var user = new User { Id = Guid.NewGuid(), Username = "temp", Email = "temp@test.com" };
        await _repository.AddAsync(user);
        await _repository.DeleteAsync(user.Id);
        var result = await _repository.GetByIdAsync(user.Id);
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindByEmailAsync_ShouldReturnUser_WhenExists()
    {
        var user = new User { Id = Guid.NewGuid(), Username = "john", Email = "john@example.com" };
        await _repository.AddAsync(user);
        var found = await _repository.FindByEmailAsync("john@example.com");
        found.Should().NotBeNull();
        found!.Username.Should().Be("john");
    }

    [Fact]
    public async Task FindByEmailAsync_ShouldReturnNull_WhenNotExists()
    {
        var found = await _repository.FindByEmailAsync("none@example.com");
        found.Should().BeNull();
    }

    [Fact]
    public async Task FindByUsernameAsync_ShouldReturnUser_WhenExists()
    {
        var user = new User { Id = Guid.NewGuid(), Username = "alice", Email = "alice@example.com" };
        await _repository.AddAsync(user);
        var found = await _repository.FindByUsernameAsync("alice");
        found.Should().NotBeNull();
        found!.Email.Should().Be("alice@example.com");
    }

    [Fact]
    public async Task GetPagedAsync_ShouldReturnPagedResult_WithSearch()
    {
        // Arrange
        for (var i = 0; i < 15; i++)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = $"user{i}",
                Email = $"user{i}@test.com"
            };
            await _repository.AddAsync(user);
        }

        // Act
        var result = await _repository.GetPagedAsync(2, 5, "user1");

        // Assert
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(5);
        result.TotalCount.Should()
            .Be(6); // user0..user15, но с поиском "user1" будут user1, user10..user15? Нет, поиск по подстроке "user1" найдёт user1, user10, user11, ... user15 (всего 7? Проверим)
        // Более простой тест: создадим конкретные
    }

    // Более точный тест для поиска
    [Fact]
    public async Task GetPagedAsync_Search_ShouldFilterCorrectly()
    {
        var users = new List<User>
        {
            new() { Id = Guid.NewGuid(), Username = "alice", Email = "alice@test.com" },
            new() { Id = Guid.NewGuid(), Username = "bob", Email = "bob@test.com" },
            new() { Id = Guid.NewGuid(), Username = "charlie", Email = "charlie@test.com" }
        };
        foreach (var u in users)
            await _repository.AddAsync(u);

        var result = await _repository.GetPagedAsync(1, 10, "bob");
        result.TotalCount.Should().Be(1);
        result.Items.Single().Username.Should().Be("bob");
    }
}