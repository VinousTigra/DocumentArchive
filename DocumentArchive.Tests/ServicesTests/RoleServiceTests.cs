using AutoMapper;
using DocumentArchive.Core.DTOs.Role;
using DocumentArchive.Core.Models;
using DocumentArchive.Services.Mapping;
using DocumentArchive.Services.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace DocumentArchive.Tests.ServicesTests;

public class RoleServiceTests : TestBase
{
    private readonly RoleService _service;
    private readonly IMapper _mapper;

    public RoleServiceTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();
        _service = new RoleService(Context, _mapper, NullLogger<RoleService>.Instance);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllRoles()
    {
        // Arrange
        // Удаляем все существующие роли (seed-данные)
        Context.Roles.RemoveRange(Context.Roles);
        await Context.SaveChangesAsync();

        var roles = new[]
        {
            new Role { Name = "RoleAdmin", Description = "Administrator" },
            new Role { Name = "RoleUser", Description = "Regular user" }
        };
        Context.Roles.AddRange(roles);
        await Context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllAsync(default);

        // Assert
        result.Should().HaveCount(2);
        result.Select(r => r.Name).Should().Contain(new[] { "RoleAdmin", "RoleUser" });
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnRole_WhenExists()
    {
        // Arrange
        var role = new Role { Name = "TestRole" };
        Context.Roles.Add(role);
        await Context.SaveChangesAsync();

        // Act
        var result = await _service.GetByIdAsync(role.Id, default);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(role.Id);
        result.Name.Should().Be("TestRole");
    }
    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = await _service.GetByIdAsync(Guid.NewGuid(), default);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ShouldAddRole()
    {
        // Arrange
        var dto = new CreateRoleDto { Name = "NewRole", Description = "Description" };

        // Act
        var result = await _service.CreateAsync(dto, default);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("NewRole");
        result.Description.Should().Be("Description");

        var role = await Context.Roles.FirstOrDefaultAsync(r => r.Name == "NewRole");
        role.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenNameExists()
    {
        // Arrange
        Context.Roles.Add(new Role { Name = "Existing" });
        await Context.SaveChangesAsync();
        var dto = new CreateRoleDto { Name = "Existing" };

        // Act
        Func<Task> act = async () => await _service.CreateAsync(dto, default);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Role with name 'Existing' already exists.");
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateRole()
    {
        // Arrange
        var role = new Role { Name = "Old", Description = "OldDesc" };
        Context.Roles.Add(role);
        await Context.SaveChangesAsync();
        var updateDto = new UpdateRoleDto { Name = "New", Description = "NewDesc" };

        // Act
        await _service.UpdateAsync(role.Id, updateDto, default);

        // Assert
        var updated = await Context.Roles.FindAsync(role.Id);
        updated!.Name.Should().Be("New");
        updated.Description.Should().Be("NewDesc");
        updated.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrow_WhenNameExists()
    {
        // Arrange
        var role1 = new Role { Name = "Role1" };
        var role2 = new Role { Name = "Role2" };
        Context.Roles.AddRange(role1, role2);
        await Context.SaveChangesAsync();
        var updateDto = new UpdateRoleDto { Name = "Role2" };

        // Act
        Func<Task> act = async () => await _service.UpdateAsync(role1.Id, updateDto, default);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Role with name 'Role2' already exists.");
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrow_WhenRoleNotFound()
    {
        // Act
        Func<Task> act = async () => await _service.UpdateAsync(Guid.NewGuid(), new UpdateRoleDto(), default);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveRole_WhenNoAssignments()
    {
        // Arrange
        var role = new Role { Name = "ToDelete" };
        Context.Roles.Add(role);
        await Context.SaveChangesAsync();

        // Act
        await _service.DeleteAsync(role.Id, default);

        // Assert
        var deleted = await Context.Roles.FindAsync(role.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrow_WhenRoleHasUserAssignments()
    {
        // Arrange
        var role = new Role { Name = "RoleWithUser" };
        var user = new User { Username = "user", Email = "u@t.com" };
        Context.Roles.Add(role);
        Context.Users.Add(user);
        await Context.SaveChangesAsync();
        Context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        await Context.SaveChangesAsync();

        // Act
        Func<Task> act = async () => await _service.DeleteAsync(role.Id, default);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot delete role because it has assigned users or permissions.");
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrow_WhenRoleHasPermissionAssignments()
    {
        // Arrange
        var role = new Role { Name = "RoleWithPermission" };
        var permission = new Permission { Name = "CanEdit" };
        Context.Roles.Add(role);
        Context.Permissions.Add(permission);
        await Context.SaveChangesAsync();
        Context.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
        await Context.SaveChangesAsync();

        // Act
        Func<Task> act = async () => await _service.DeleteAsync(role.Id, default);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot delete role because it has assigned users or permissions.");
    }

    [Fact]
    public async Task AssignPermissionAsync_ShouldAddPermissionToRole()
    {
        // Arrange
        var role = new Role { Name = "TestRole" };
        var permission = new Permission { Name = "CanEdit" };
        Context.Roles.Add(role);
        Context.Permissions.Add(permission);
        await Context.SaveChangesAsync();

        // Act
        await _service.AssignPermissionAsync(role.Id, permission.Id, default);

        // Assert
        var rp = await Context.RolePermissions
            .FirstOrDefaultAsync(rp => rp.RoleId == role.Id && rp.PermissionId == permission.Id);
        rp.Should().NotBeNull();
    }

    [Fact]
    public async Task AssignPermissionAsync_ShouldThrow_WhenAlreadyAssigned()
    {
        // Arrange
        var role = new Role { Name = "TestRole" };
        var permission = new Permission { Name = "CanEdit" };
        Context.Roles.Add(role);
        Context.Permissions.Add(permission);
        await Context.SaveChangesAsync();
        Context.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
        await Context.SaveChangesAsync();

        // Act
        Func<Task> act = async () => await _service.AssignPermissionAsync(role.Id, permission.Id, default);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Permission already assigned to this role.");
    }

    [Fact]
    public async Task AssignPermissionAsync_ShouldThrow_WhenRoleOrPermissionNotFound()
    {
        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.AssignPermissionAsync(Guid.NewGuid(), Guid.NewGuid(), default));
    }

    [Fact]
    public async Task RemovePermissionAsync_ShouldRemovePermissionFromRole()
    {
        // Arrange
        var role = new Role { Name = "TestRole" };
        var permission = new Permission { Name = "CanEdit" };
        Context.Roles.Add(role);
        Context.Permissions.Add(permission);
        await Context.SaveChangesAsync();
        Context.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
        await Context.SaveChangesAsync();

        // Act
        await _service.RemovePermissionAsync(role.Id, permission.Id, default);

        // Assert
        var rp = await Context.RolePermissions
            .FirstOrDefaultAsync(rp => rp.RoleId == role.Id && rp.PermissionId == permission.Id);
        rp.Should().BeNull();
    }

    [Fact]
    public async Task RemovePermissionAsync_ShouldThrow_WhenNotAssigned()
    {
        // Arrange
        var role = new Role { Name = "TestRole" };
        var permission = new Permission { Name = "CanEdit" };
        Context.Roles.Add(role);
        Context.Permissions.Add(permission);
        await Context.SaveChangesAsync();

        // Act
        Func<Task> act = async () => await _service.RemovePermissionAsync(role.Id, permission.Id, default);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Permission not assigned to this role.");
    }
}