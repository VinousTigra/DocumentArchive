using AutoMapper;
using DocumentArchive.Core.DTOs.Permission;
using DocumentArchive.Core.Models;
using DocumentArchive.Services.Mapping;
using DocumentArchive.Services.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace DocumentArchive.Tests.ServicesTests;

public class PermissionServiceTests : TestBase
{
    private readonly PermissionService _service;
    private readonly IMapper _mapper;

    public PermissionServiceTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();
        _service = new PermissionService(Context, _mapper, NullLogger<PermissionService>.Instance);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllPermissions()
    {
        // Arrange
        var permissions = new[]
        {
            new Permission { Name = "CanEdit", Category = "Docs" },
            new Permission { Name = "CanDelete", Category = "Docs" }
        };
        Context.Permissions.AddRange(permissions);
        await Context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllAsync(default);

        // Assert
        result.Should().HaveCount(2);
        result.Select(p => p.Name).Should().Contain(new[] { "CanEdit", "CanDelete" });
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnPermission_WhenExists()
    {
        // Arrange
        var permission = new Permission { Name = "CanEdit" };
        Context.Permissions.Add(permission);
        await Context.SaveChangesAsync();

        // Act
        var result = await _service.GetByIdAsync(permission.Id, default);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(permission.Id);
        result.Name.Should().Be("CanEdit");
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
    public async Task CreateAsync_ShouldAddPermission()
    {
        // Arrange
        var dto = new CreatePermissionDto { Name = "NewPerm", Description = "Desc", Category = "Cat" };

        // Act
        var result = await _service.CreateAsync(dto, default);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("NewPerm");
        result.Description.Should().Be("Desc");
        result.Category.Should().Be("Cat");

        var permission = await Context.Permissions.FirstOrDefaultAsync(p => p.Name == "NewPerm");
        permission.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenNameExists()
    {
        // Arrange
        Context.Permissions.Add(new Permission { Name = "Existing" });
        await Context.SaveChangesAsync();
        var dto = new CreatePermissionDto { Name = "Existing" };

        // Act
        Func<Task> act = async () => await _service.CreateAsync(dto, default);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Permission with name 'Existing' already exists.");
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdatePermission()
    {
        // Arrange
        var permission = new Permission { Name = "Old", Description = "OldDesc", Category = "OldCat" };
        Context.Permissions.Add(permission);
        await Context.SaveChangesAsync();
        var updateDto = new UpdatePermissionDto { Name = "New", Description = "NewDesc", Category = "NewCat" };

        // Act
        await _service.UpdateAsync(permission.Id, updateDto, default);

        // Assert
        var updated = await Context.Permissions.FindAsync(permission.Id);
        updated!.Name.Should().Be("New");
        updated.Description.Should().Be("NewDesc");
        updated.Category.Should().Be("NewCat");
        updated.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrow_WhenNameExists()
    {
        // Arrange
        var perm1 = new Permission { Name = "Perm1" };
        var perm2 = new Permission { Name = "Perm2" };
        Context.Permissions.AddRange(perm1, perm2);
        await Context.SaveChangesAsync();
        var updateDto = new UpdatePermissionDto { Name = "Perm2" };

        // Act
        Func<Task> act = async () => await _service.UpdateAsync(perm1.Id, updateDto, default);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Permission with name 'Perm2' already exists.");
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrow_WhenPermissionNotFound()
    {
        // Act
        Func<Task> act = async () => await _service.UpdateAsync(Guid.NewGuid(), new UpdatePermissionDto(), default);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemovePermission_WhenNoAssignments()
    {
        // Arrange
        var permission = new Permission { Name = "ToDelete" };
        Context.Permissions.Add(permission);
        await Context.SaveChangesAsync();

        // Act
        await _service.DeleteAsync(permission.Id, default);

        // Assert
        var deleted = await Context.Permissions.FindAsync(permission.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrow_WhenPermissionHasRoleAssignments()
    {
        // Arrange
        var permission = new Permission { Name = "CanEdit" };
        var role = new Role { Name = "TestRole" }; // изменено с "Admin"
        Context.Permissions.Add(permission);
        Context.Roles.Add(role);
        await Context.SaveChangesAsync();
        Context.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
        await Context.SaveChangesAsync();

        // Act
        Func<Task> act = async () => await _service.DeleteAsync(permission.Id, default);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot delete permission because it is assigned to roles.");
    }
}