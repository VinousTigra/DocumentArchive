using DocumentArchive.Core.DTOs.Role;
using DocumentArchive.Core.Interfaces.Services;
using DocumentArchive.Core.Models;
using DocumentArchive.Services.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DocumentArchive.Tests.ServicesTests;

public class RoleServiceTests :
    TestBase
{
    private readonly Mock<IAuditService> _auditMock;
    private readonly RoleService _service;

    public RoleServiceTests()
    {
        var mapper = TestHelper.CreateMapper();
        _auditMock = new Mock<IAuditService>();
        _service = new RoleService(Context, mapper, NullLogger<RoleService>.Instance, _auditMock.Object);
    }

    protected override void SeedData()
    {
        Context.Roles.AddRange(
            new Role { Id = Guid.NewGuid(), Name = "Admin", Description = "Administrator" },
            new Role { Id = Guid.NewGuid(), Name = "User", Description = "Regular user" }
        );
        Context.SaveChanges();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllRoles()
    {
        var result = await _service.GetAllAsync(default);
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ShouldReturnRole()
    {
        var id = Context.Roles.First().Id;
        var result = await _service.GetByIdAsync(id, default);
        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ShouldReturnNull()
    {
        var result = await _service.GetByIdAsync(Guid.NewGuid(), default);
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ShouldCreateRoleAndLog()
    {
        var dto = new CreateRoleDto { Name = "Moderator", Description = "Moderator role" };
        var result = await _service.CreateAsync(dto, default);
        result.Name.Should().Be("Moderator");
        Context.Roles.Count().Should().Be(3);
        _auditMock.Verify(x => x.LogAsync(
            SecurityEventType.RoleCreated,
            null,
            null,
            null,
            null,
            true,
            It.Is<object>(o => o.ToString()!.Contains("Moderator"))), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ShouldThrow()
    {
        var dto = new CreateRoleDto { Name = "Admin" };
        await FluentActions.Invoking(() => _service.CreateAsync(dto, default))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task UpdateAsync_ValidDto_ShouldUpdateRoleAndLog()
    {
        var id = Context.Roles.First(r => r.Name == "User").Id;
        var dto = new UpdateRoleDto { Name = "UpdatedUser", Description = "New desc" };
        await _service.UpdateAsync(id, dto, default);
        var updated = await Context.Roles.FindAsync(id);
        updated!.Name.Should().Be("UpdatedUser");
        updated.Description.Should().Be("New desc");
        _auditMock.Verify(x => x.LogAsync(
            SecurityEventType.RoleUpdated,
            null,
            null,
            null,
            null,
            true,
            It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingId_ShouldThrow()
    {
        var dto = new UpdateRoleDto { Name = "Test" };
        await FluentActions.Invoking(() => _service.UpdateAsync(Guid.NewGuid(), dto, default))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_DuplicateName_ShouldThrow()
    {
        var roles = Context.Roles.ToList();
        var id = roles[0].Id;
        var dto = new UpdateRoleDto { Name = roles[1].Name };
        await FluentActions.Invoking(() => _service.UpdateAsync(id, dto, default))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task DeleteAsync_WithoutDependencies_ShouldDeleteAndLog()
    {
        // добавим роль без зависимостей
        var role = new Role { Id = Guid.NewGuid(), Name = "Temp", Description = "Temp" };
        Context.Roles.Add(role);
        await Context.SaveChangesAsync();
        await _service.DeleteAsync(role.Id, default);
        Context.Roles.Count().Should().Be(2); // исходно 2 + temp - удалённая = 2
        _auditMock.Verify(x => x.LogAsync(
            SecurityEventType.RoleDeleted,
            null,
            null,
            null,
            null,
            true,
            It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithUserRoles_ShouldThrow()
    {
        var role = Context.Roles.First();
        Context.UserRoles.Add(new UserRole { UserId = Guid.NewGuid(), RoleId = role.Id });
        await Context.SaveChangesAsync();
        await FluentActions.Invoking(() => _service.DeleteAsync(role.Id, default))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*assigned users or permissions*");
    }

    [Fact]
    public async Task AssignPermissionAsync_ShouldAddRelationAndLog()
    {
        var roleId = Context.Roles.First().Id;
        var permId = Guid.NewGuid();
        Context.Permissions.Add(new Permission { Id = permId, Name = "TestPerm" });
        await Context.SaveChangesAsync();
        await _service.AssignPermissionAsync(roleId, permId, default);
        Context.RolePermissions.Count().Should().Be(1);
        _auditMock.Verify(x => x.LogAsync(
            SecurityEventType.PermissionAssigned,
            null,
            null,
            null,
            null,
            true,
            It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task AssignPermissionAsync_AlreadyAssigned_ShouldThrow()
    {
        var roleId = Context.Roles.First().Id;
        var permId = Guid.NewGuid();
        Context.Permissions.Add(new Permission { Id = permId, Name = "TestPerm" });
        Context.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permId });
        await Context.SaveChangesAsync();
        await FluentActions.Invoking(() => _service.AssignPermissionAsync(roleId, permId, default))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already assigned*");
    }

    [Fact]
    public async Task RemovePermissionAsync_ShouldRemoveRelationAndLog()
    {
        var roleId = Context.Roles.First().Id;
        var permId = Guid.NewGuid();
        Context.Permissions.Add(new Permission { Id = permId, Name = "TestPerm" });
        Context.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permId });
        await Context.SaveChangesAsync();
        await _service.RemovePermissionAsync(roleId, permId, default);
        Context.RolePermissions.Count().Should().Be(0);
        _auditMock.Verify(x => x.LogAsync(
            SecurityEventType.PermissionRevoked,
            null,
            null,
            null,
            null,
            true,
            It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task RemovePermissionAsync_NotAssigned_ShouldThrow()
    {
        var roleId = Context.Roles.First().Id;
        var permId = Guid.NewGuid();
        Context.Permissions.Add(new Permission { Id = permId, Name = "TestPerm" });
        await Context.SaveChangesAsync();
        await FluentActions.Invoking(() => _service.RemovePermissionAsync(roleId, permId, default))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*not assigned*");
    }
}