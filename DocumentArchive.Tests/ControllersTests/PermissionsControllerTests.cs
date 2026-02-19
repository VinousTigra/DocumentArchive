using DocumentArchive.API.Controllers;
using DocumentArchive.Core.DTOs.Permission;
using DocumentArchive.Core.Interfaces.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DocumentArchive.Tests.ControllersTests;

public class PermissionsControllerTests
{
    private readonly PermissionsController _controller;
    private readonly Mock<IPermissionService> _permissionServiceMock;

    public PermissionsControllerTests()
    {
        _permissionServiceMock = new Mock<IPermissionService>();
        _controller = new PermissionsController(_permissionServiceMock.Object);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOk_WithListOfPermissions()
    {
        var permissions = new List<PermissionListItemDto> { new() { Id = Guid.NewGuid(), Name = "CanEdit" } };
        _permissionServiceMock.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);

        var result = await _controller.GetAll(default);
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(permissions);
    }

    [Fact]
    public async Task GetById_ShouldReturnOk_WhenPermissionExists()
    {
        var id = Guid.NewGuid();
        var permission = new PermissionResponseDto { Id = id, Name = "CanEdit" };
        _permissionServiceMock.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permission);

        var result = await _controller.GetById(id, default);
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(permission);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenPermissionDoesNotExist()
    {
        var id = Guid.NewGuid();
        _permissionServiceMock.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PermissionResponseDto?)null);

        var result = await _controller.GetById(id, default);
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Create_ShouldReturnCreated_WhenValid()
    {
        var dto = new CreatePermissionDto { Name = "NewPermission" };
        var created = new PermissionResponseDto { Id = Guid.NewGuid(), Name = "NewPermission" };
        _permissionServiceMock.Setup(x => x.CreateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        var result = await _controller.Create(dto, default);
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult.Should().NotBeNull();
        createdResult!.StatusCode.Should().Be(201);
        createdResult.ActionName.Should().Be(nameof(_controller.GetById));
        createdResult.Value.Should().Be(created);
    }

    [Fact]
    public async Task Create_ShouldThrowInvalidOperationException_WhenServiceThrows()
    {
        var dto = new CreatePermissionDto { Name = "Existing" };
        _permissionServiceMock.Setup(x => x.CreateAsync(dto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Permission with this name already exists."));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _controller.Create(dto, default));
    }

    [Fact]
    public async Task Update_ShouldReturnNoContent_WhenValid()
    {
        var id = Guid.NewGuid();
        var dto = new UpdatePermissionDto { Name = "Updated" };
        _permissionServiceMock.Setup(x => x.UpdateAsync(id, dto, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.Update(id, dto, default);
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Update_ShouldThrowKeyNotFoundException_WhenPermissionNotFound()
    {
        var id = Guid.NewGuid();
        var dto = new UpdatePermissionDto { Name = "Updated" };
        _permissionServiceMock.Setup(x => x.UpdateAsync(id, dto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException());

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.Update(id, dto, default));
    }

    [Fact]
    public async Task Delete_ShouldReturnNoContent_WhenSuccess()
    {
        var id = Guid.NewGuid();
        _permissionServiceMock.Setup(x => x.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.Delete(id, default);
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_ShouldThrowKeyNotFoundException_WhenPermissionNotFound()
    {
        var id = Guid.NewGuid();
        _permissionServiceMock.Setup(x => x.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException());

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.Delete(id, default));
    }
}