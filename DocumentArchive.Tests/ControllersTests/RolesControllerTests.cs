using DocumentArchive.API.Controllers;
using DocumentArchive.Core.DTOs.Role;
using DocumentArchive.Core.Interfaces.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DocumentArchive.Tests.ControllersTests;

public class RolesControllerTests
{
    private readonly RolesController _controller;
    private readonly Mock<IRoleService> _roleServiceMock;

    public RolesControllerTests()
    {
        _roleServiceMock = new Mock<IRoleService>();
        _controller = new RolesController(_roleServiceMock.Object);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOk_WithListOfRoles()
    {
        var roles = new List<RoleListItemDto> { new() { Id = Guid.NewGuid(), Name = "Admin" } };
        _roleServiceMock.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);

        var result = await _controller.GetAll(default);
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(roles);
    }

    [Fact]
    public async Task GetById_ShouldReturnOk_WhenRoleExists()
    {
        var id = Guid.NewGuid();
        var role = new RoleResponseDto { Id = id, Name = "Admin" };
        _roleServiceMock.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        var result = await _controller.GetById(id, default);
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(role);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenRoleDoesNotExist()
    {
        var id = Guid.NewGuid();
        _roleServiceMock.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoleResponseDto?)null);

        var result = await _controller.GetById(id, default);
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Create_ShouldReturnCreated_WhenValid()
    {
        var dto = new CreateRoleDto { Name = "NewRole" };
        var createdRole = new RoleResponseDto { Id = Guid.NewGuid(), Name = "NewRole" };
        _roleServiceMock.Setup(x => x.CreateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdRole);

        var result = await _controller.Create(dto, default);
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult.Should().NotBeNull();
        createdResult!.StatusCode.Should().Be(201);
        createdResult.ActionName.Should().Be(nameof(_controller.GetById));
        createdResult.Value.Should().Be(createdRole);
    }

    [Fact]
    public async Task Create_ShouldThrowInvalidOperationException_WhenServiceThrows()
    {
        var dto = new CreateRoleDto { Name = "ExistingRole" };
        _roleServiceMock.Setup(x => x.CreateAsync(dto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Role with this name already exists."));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _controller.Create(dto, default));
    }

    [Fact]
    public async Task Update_ShouldReturnNoContent_WhenValid()
    {
        var id = Guid.NewGuid();
        var dto = new UpdateRoleDto { Name = "Updated" };
        _roleServiceMock.Setup(x => x.UpdateAsync(id, dto, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.Update(id, dto, default);
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Update_ShouldThrowKeyNotFoundException_WhenRoleNotFound()
    {
        var id = Guid.NewGuid();
        var dto = new UpdateRoleDto { Name = "Updated" };
        _roleServiceMock.Setup(x => x.UpdateAsync(id, dto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException());

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.Update(id, dto, default));
    }

    [Fact]
    public async Task Delete_ShouldReturnNoContent_WhenSuccess()
    {
        var id = Guid.NewGuid();
        _roleServiceMock.Setup(x => x.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.Delete(id, default);
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_ShouldThrowKeyNotFoundException_WhenRoleNotFound()
    {
        var id = Guid.NewGuid();
        _roleServiceMock.Setup(x => x.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException());

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.Delete(id, default));
    }

    [Fact]
    public async Task AssignPermission_ShouldReturnOk_WhenSuccess()
    {
        var roleId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        _roleServiceMock.Setup(x => x.AssignPermissionAsync(roleId, permissionId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.AssignPermission(roleId, permissionId, default);
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task AssignPermission_ShouldThrowInvalidOperationException_WhenServiceThrows()
    {
        var roleId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        _roleServiceMock.Setup(x => x.AssignPermissionAsync(roleId, permissionId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Permission already assigned."));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _controller.AssignPermission(roleId, permissionId, default));
    }

    [Fact]
    public async Task RemovePermission_ShouldReturnNoContent_WhenSuccess()
    {
        var roleId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        _roleServiceMock.Setup(x => x.RemovePermissionAsync(roleId, permissionId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.RemovePermission(roleId, permissionId, default);
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task RemovePermission_ShouldThrowKeyNotFoundException_WhenServiceThrows()
    {
        var roleId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        _roleServiceMock.Setup(x => x.RemovePermissionAsync(roleId, permissionId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException());

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _controller.RemovePermission(roleId, permissionId, default));
    }
}