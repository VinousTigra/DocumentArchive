using AutoMapper;
using DocumentArchive.Core.DTOs.User;
using DocumentArchive.Core.Models;
using DocumentArchive.Services.Mapping;
using DocumentArchive.Services.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace DocumentArchive.Tests.ServicesTests;

public class UserServiceTests : TestBase
{
    private readonly UserService _service;

    public UserServiceTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        var mapper = config.CreateMapper();
        _service = new UserService(Context, mapper, NullLogger<UserService>.Instance);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldAddUser()
    {
        // Arrange
        var dto = new CreateUserDto
        {
            Username = "newuser",
            Email = "new@test.com"
        };

        // Act
        var result = await _service.CreateUserAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Username.Should().Be("newuser");
        result.Email.Should().Be("new@test.com");

        var user = await Context.Users.FirstOrDefaultAsync(u => u.Email == "new@test.com");
        user.Should().NotBeNull();
        user.IsActive.Should().BeTrue();
        user.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task CreateUserAsync_ShouldThrow_WhenEmailExists()
    {
        // Arrange
        var existing = new User { Id = Guid.NewGuid(), Username = "old", Email = "dup@test.com" };
        Context.Users.Add(existing);
        await Context.SaveChangesAsync();

        var dto = new CreateUserDto { Username = "new", Email = "dup@test.com" };

        // Act
        Func<Task> act = async () => await _service.CreateUserAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("User with email 'dup@test.com' already exists.");
    }

    [Fact]
    public async Task CreateUserAsync_ShouldThrow_WhenUsernameExists()
    {
        // Arrange
        var existing = new User { Id = Guid.NewGuid(), Username = "dupuser", Email = "orig@test.com" };
        Context.Users.Add(existing);
        await Context.SaveChangesAsync();

        var dto = new CreateUserDto { Username = "dupuser", Email = "new@test.com" };

        // Act
        Func<Task> act = async () => await _service.CreateUserAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("User with username 'dupuser' already exists.");
    }

    [Fact]
    public async Task GetUserByIdAsync_ShouldReturnDto_WhenExists()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@t.com",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserByIdAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(user.Id);
        result.Username.Should().Be("testuser");
        result.Email.Should().Be("test@t.com");
        result.CreatedAt.Should().BeCloseTo(user.CreatedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetUserByIdAsync_ShouldReturnNull_WhenDeleted()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "del",
            Email = "del@t.com",
            IsDeleted = true
        };
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserByIdAsync(user.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldUpdateFields()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "old",
            Email = "old@t.com"
        };
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        var updateDto = new UpdateUserDto
        {
            Username = "new",
            Email = "new@t.com"
        };

        // Act
        await _service.UpdateUserAsync(user.Id, updateDto);

        // Assert
        var updated = await Context.Users.FindAsync(user.Id);
        updated!.Username.Should().Be("new");
        updated.Email.Should().Be("new@t.com");
        updated.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldThrow_WhenEmailExists()
    {
        // Arrange
        var user1 = new User { Id = Guid.NewGuid(), Username = "u1", Email = "u1@t.com" };
        var user2 = new User { Id = Guid.NewGuid(), Username = "u2", Email = "u2@t.com" };
        Context.Users.AddRange(user1, user2);
        await Context.SaveChangesAsync();

        var updateDto = new UpdateUserDto { Email = "u2@t.com" }; // пытаемся дать user1 email user2

        // Act
        var act = async () => await _service.UpdateUserAsync(user1.Id, updateDto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"User with email '{updateDto.Email}' already exists.");
    }

    [Fact]
    public async Task DeleteUserAsync_ShouldSoftDelete()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "todelete",
            Email = "del@t.com",
            IsDeleted = false
        };
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        // Act
        await _service.DeleteUserAsync(user.Id);

        // Assert
        var deleted = await Context.Users.FindAsync(user.Id);
        deleted!.IsDeleted.Should().BeTrue();
        deleted.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteUserAsync_ShouldThrow_WhenHasDocuments()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Username = "u", Email = "u@t.com" };
        Context.Users.Add(user);
        var doc = new Document { Id = Guid.NewGuid(), Title = "Doc", UserId = user.Id };
        Context.Documents.Add(doc);
        await Context.SaveChangesAsync();

        // Act
        var act = async () => await _service.DeleteUserAsync(user.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot delete user with existing documents.");
    }

    [Fact]
    public async Task GetUsersAsync_ShouldReturnPagedFiltered()
    {
        // Arrange
        for (var i = 1; i <= 5; i++)
            Context.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                Username = $"user{i}",
                Email = $"user{i}@t.com",
                IsDeleted = false
            });
        // Добавим удалённого, он не должен попасть в выборку
        Context.Users.Add(new User
            { Id = Guid.NewGuid(), Username = "deleted", Email = "del@t.com", IsDeleted = true });

        await Context.SaveChangesAsync(); // <-- добавить эту строку

        // Act
        var result = await _service.GetUsersAsync(1, 3, null);

        // Assert
        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(5);

        // Поиск
        var search = await _service.GetUsersAsync(1, 10, "user2");
        search.Items.Should().HaveCount(1);
        search.Items.First().Username.Should().Be("user2");
    }

    [Fact]
    public async Task GetUserStatisticsAsync_ShouldReturnCorrectData()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "stat",
            Email = "stat@t.com",
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            LastLoginAt = DateTime.UtcNow.AddHours(-1)
        };
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        for (var i = 0; i < 7; i++) Context.Documents.Add(new Document { Title = $"Doc{i}", UserId = user.Id });
        await Context.SaveChangesAsync();

        // Act
        var stats = await _service.GetUserStatisticsAsync(user.Id);

        // Assert
        stats.Should().NotBeNull();
        stats.UserId.Should().Be(user.Id);
        stats.Username.Should().Be("stat");
        stats.Email.Should().Be("stat@t.com");
        stats.DocumentsCount.Should().Be(7);
        stats.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow.AddHours(-1), TimeSpan.FromSeconds(5));
        stats.RegisteredAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(-10), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetUsersGeneralStatisticsAsync_ShouldReturnSummary()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        Context.Users.AddRange(
            new User
            {
                Username = "user1", Email = "user1@test.com", CreatedAt = today.AddDays(-1),
                LastLoginAt = DateTime.UtcNow, IsDeleted = false
            },
            new User
            {
                Username = "user2", Email = "user2@test.com", CreatedAt = today.AddDays(-1),
                LastLoginAt = DateTime.UtcNow.AddDays(-1), IsDeleted = false
            },
            new User
            {
                Username = "user3", Email = "user3@test.com", CreatedAt = today, LastLoginAt = null, IsDeleted = false
            },
            new User
            {
                Username = "user4", Email = "user4@test.com", CreatedAt = today.AddDays(-5),
                LastLoginAt = DateTime.UtcNow, IsDeleted = true
            } // не должен считаться
        );
        await Context.SaveChangesAsync();

        // Act
        var stats = await _service.GetUsersGeneralStatisticsAsync();

        // Assert
        stats.TotalUsers.Should().Be(3);
        stats.ActiveToday.Should().Be(2); // два пользователя с lastlogin сегодня
        stats.UsersByRegistrationDate.Should().HaveCount(2); // дни: вчера и сегодня
    }

    [Fact]
    public async Task GetUserDocumentsAsync_ShouldReturnPaged()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Username = "u", Email = "u@t.com" };
        Context.Users.Add(user);
        for (var i = 0; i < 10; i++) Context.Documents.Add(new Document { Title = $"Doc{i}", UserId = user.Id });
        await Context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserDocumentsAsync(user.Id, 2, 3);

        // Assert
        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(10);
        result.PageNumber.Should().Be(2);
    }
}