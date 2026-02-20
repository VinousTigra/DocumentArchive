using DocumentArchive.Core.DTOs.Permission;
using DocumentArchive.Core.Models;
using DocumentArchive.Services.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace DocumentArchive.Tests.ServicesTests;

public class PermissionServiceTests : TestBase
{
    private readonly PermissionService _service;

    public PermissionServiceTests()
    {
        var mapper = TestHelper.CreateMapper();
        _service = new PermissionService(Context, mapper, NullLogger<PermissionService>.Instance);
    }

    protected override void SeedData()
    {
        Context.Permissions.AddRange(
            new Permission
                { Id = Guid.NewGuid(), Name = "View", Description = "View documents", Category = "Documents" },
            new Permission
                { Id = Guid.NewGuid(), Name = "Edit", Description = "Edit documents", Category = "Documents" }
        );
        Context.SaveChanges();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllPermissions()
    {
        var result = await _service.GetAllAsync(default);
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ShouldReturnPermission()
    {
        var id = Context.Permissions.First().Id;
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
    public async Task CreateAsync_ValidDto_ShouldCreatePermission()
    {
        var dto = new CreatePermissionDto { Name = "Delete", Description = "Delete documents", Category = "Documents" };
        var result = await _service.CreateAsync(dto, default);
        result.Name.Should().Be("Delete");
        Context.Permissions.Count().Should().Be(3);
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ShouldThrow()
    {
        var dto = new CreatePermissionDto { Name = "View", Description = "Duplicate" };
        await FluentActions.Invoking(() => _service.CreateAsync(dto, default))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task UpdateAsync_ValidDto_ShouldUpdatePermission()
    {
        var id = Context.Permissions.First().Id;
        var dto = new UpdatePermissionDto { Name = "UpdatedName" };
        await _service.UpdateAsync(id, dto, default);
        var updated = await Context.Permissions.FindAsync(id);
        updated!.Name.Should().Be("UpdatedName");
    }

    [Fact]
    public async Task UpdateAsync_NonExistingId_ShouldThrow()
    {
        var dto = new UpdatePermissionDto { Name = "Test" };
        await FluentActions.Invoking(() => _service.UpdateAsync(Guid.NewGuid(), dto, default))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_DuplicateName_ShouldThrow()
    {
        var permissions = Context.Permissions.ToList();
        var id = permissions[0].Id;
        var dto = new UpdatePermissionDto { Name = permissions[1].Name };
        await FluentActions.Invoking(() => _service.UpdateAsync(id, dto, default))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task DeleteAsync_WithoutDependencies_ShouldDelete()
    {
        var id = Context.Permissions.First().Id;
        await _service.DeleteAsync(id, default);
        Context.Permissions.Count().Should().Be(1);
    }

    [Fact]
    public async Task DeleteAsync_WithRolePermissions_ShouldThrow()
    {
        var perm = Context.Permissions.First();
        Context.RolePermissions.Add(new RolePermission { RoleId = Guid.NewGuid(), PermissionId = perm.Id });
        await Context.SaveChangesAsync();
        await FluentActions.Invoking(() => _service.DeleteAsync(perm.Id, default))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*assigned to roles*");
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_ShouldThrow()
    {
        await FluentActions.Invoking(() => _service.DeleteAsync(Guid.NewGuid(), default))
            .Should().ThrowAsync<KeyNotFoundException>();
    }
}