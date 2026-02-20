using DocumentArchive.Core.DTOs.User;
using DocumentArchive.Core.Interfaces.Services;
using DocumentArchive.Core.Models;
using DocumentArchive.Services.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DocumentArchive.Tests.ServicesTests;

public class UserServiceTests : TestBase
{
    private readonly Mock<IAuditService> _auditMock;
    private readonly UserService _service;

    public UserServiceTests()
    {
        var mapper = TestHelper.CreateMapper();
        _auditMock = new Mock<IAuditService>();
        _service = new UserService(Context, mapper, NullLogger<UserService>.Instance, _auditMock.Object);
    }

    protected override void SeedData()
    {
        var users = new[]
        {
            new User
            {
                Id = Guid.NewGuid(), Username = "user1", Email = "user1@test.com", FirstName = "John", LastName = "Doe",
                IsDeleted = false, CreatedAt = DateTime.UtcNow.AddDays(-10)
            },
            new User
            {
                Id = Guid.NewGuid(), Username = "user2", Email = "user2@test.com", IsDeleted = false,
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            },
            new User
            {
                Id = Guid.NewGuid(), Username = "deleted", Email = "deleted@test.com", IsDeleted = true,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            }
        };
        Context.Users.AddRange(users);
        Context.SaveChanges();
    }

    [Fact]
    public async Task GetUsersAsync_WithoutSearch_ShouldReturnActiveUsers()
    {
        var result = await _service.GetUsersAsync(1, 10, null);
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetUsersAsync_WithSearch_ShouldFilter()
    {
        var result = await _service.GetUsersAsync(1, 10, "user1");
        result.Items.Should().HaveCount(1);
        result.Items.First().Username.Should().Be("user1");
    }

    [Fact]
    public async Task GetUserByIdAsync_ExistingUser_ShouldReturn()
    {
        var id = Context.Users.First(u => u.Username == "user1").Id;
        var result = await _service.GetUserByIdAsync(id);
        result.Should().NotBeNull();
        result!.Username.Should().Be("user1");
    }

    [Fact]
    public async Task GetUserByIdAsync_DeletedUser_ShouldReturnNull()
    {
        var id = Context.Users.First(u => u.IsDeleted).Id;
        var result = await _service.GetUserByIdAsync(id);
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateUserAsync_ValidDto_ShouldCreateUserAndLog()
    {
        var dto = new CreateUserDto { Username = "newuser", Email = "new@test.com" };
        var result = await _service.CreateUserAsync(dto);
        result.Username.Should().Be("newuser");
        Context.Users.Count().Should().Be(4); // было 3, стало 4
        _auditMock.Verify(x => x.LogAsync(
            SecurityEventType.UserCreated,
            result.Id,
            "new@test.com",
            null,
            null,
            true,
            It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task CreateUserAsync_DuplicateEmail_ShouldThrow()
    {
        var dto = new CreateUserDto { Username = "unique", Email = "user1@test.com" };
        await FluentActions.Invoking(() => _service.CreateUserAsync(dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*email*already exists*");
    }

    [Fact]
    public async Task UpdateUserAsync_ValidUpdate_ShouldUpdate()
    {
        var user = Context.Users.First(u => u.Username == "user1");
        var dto = new UpdateUserDto { Username = "updated", Email = "updated@test.com" };
        await _service.UpdateUserAsync(user.Id, dto);
        var updated = await Context.Users.FindAsync(user.Id);
        updated!.Username.Should().Be("updated");
        updated.Email.Should().Be("updated@test.com");
        _auditMock.Verify(x => x.LogAsync(
            SecurityEventType.UserUpdated,
            user.Id,
            "updated@test.com",
            null,
            null,
            true,
            It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task DeleteUserAsync_WithoutDocuments_ShouldSoftDelete()
    {
        var user = new User { Id = Guid.NewGuid(), Username = "temp", Email = "temp@test.com", IsDeleted = false };
        Context.Users.Add(user);
        await Context.SaveChangesAsync();
        await _service.DeleteUserAsync(user.Id);
        var deleted = await Context.Users.FindAsync(user.Id);
        deleted!.IsDeleted.Should().BeTrue();
        _auditMock.Verify(x => x.LogAsync(
            SecurityEventType.UserDeleted,
            user.Id,
            user.Email,
            null,
            null,
            true,
            It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task DeleteUserAsync_WithDocuments_ShouldThrow()
    {
        var user = Context.Users.First(u => u.Username == "user1");
        Context.Documents.Add(new Document { Id = Guid.NewGuid(), Title = "Doc", UserId = user.Id });
        await Context.SaveChangesAsync();
        await FluentActions.Invoking(() => _service.DeleteUserAsync(user.Id))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*existing documents*");
    }

    [Fact]
    public async Task AssignRoleAsync_ShouldAddRoleAndLog()
    {
        var user = Context.Users.First();
        var role = new Role { Id = Guid.NewGuid(), Name = "TestRole" };
        Context.Roles.Add(role);
        await Context.SaveChangesAsync();
        await _service.AssignRoleAsync(user.Id, role.Id);
        Context.UserRoles.Count().Should().Be(1);
        _auditMock.Verify(x => x.LogAsync(
            SecurityEventType.RoleAssigned,
            user.Id,
            user.Email,
            null,
            null,
            true,
            It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task RemoveRoleAsync_ShouldRemoveAndLog()
    {
        var user = Context.Users.First();
        var role = new Role { Id = Guid.NewGuid(), Name = "TestRole" };
        Context.Roles.Add(role);
        Context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        await Context.SaveChangesAsync();
        await _service.RemoveRoleAsync(user.Id, role.Id);
        Context.UserRoles.Count().Should().Be(0);
        _auditMock.Verify(x => x.LogAsync(
            SecurityEventType.RoleRevoked,
            user.Id,
            null,
            null,
            null,
            true,
            It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task GetUserDocumentsAsync_ShouldReturnPaged()
    {
        var user = Context.Users.First();
        for (var i = 0; i < 5; i++)
            Context.Documents.Add(new Document { Id = Guid.NewGuid(), Title = $"Doc{i}", UserId = user.Id });
        await Context.SaveChangesAsync();
        var result = await _service.GetUserDocumentsAsync(user.Id, 1, 3);
        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(5);
    }

    [Fact]
    public async Task GetUserStatisticsAsync_ShouldReturnStats()
    {
        var user = Context.Users.First();
        for (var i = 0; i < 3; i++)
            Context.Documents.Add(new Document { Id = Guid.NewGuid(), Title = $"Doc{i}", UserId = user.Id });
        await Context.SaveChangesAsync();
        var stats = await _service.GetUserStatisticsAsync(user.Id);
        stats.DocumentsCount.Should().Be(3);
        stats.Username.Should().Be(user.Username);
    }

    [Fact]
    public async Task GetUsersGeneralStatisticsAsync_ShouldReturnCorrectCounts()
    {
        var stats = await _service.GetUsersGeneralStatisticsAsync();
        stats.TotalUsers.Should().Be(2); // только активные
        stats.ActiveToday.Should().Be(0); // last login не задан
        stats.UsersByRegistrationDate.Should().HaveCount(2);
    }
}