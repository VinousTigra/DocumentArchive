using AutoMapper;
using DocumentArchive.API.Controllers;
using DocumentArchive.Core.DTOs.Document;
using DocumentArchive.Core.DTOs.Shared;
using DocumentArchive.Core.DTOs.User;
using DocumentArchive.Core.Interfaces.Services;
using DocumentArchive.Core.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace DocumentArchive.Tests.ControllersTests;

public class UsersControllerTests
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<ILogger<UsersController>> _loggerMock;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _userServiceMock = new Mock<IUserService>();
        _loggerMock = new Mock<ILogger<UsersController>>();
        _controller = new UsersController(_userServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetById_ShouldReturnOk_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var responseDto = new UserResponseDto { Id = userId, Username = "test" };
        _userServiceMock.Setup(x => x.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        // Act
        var result = await _controller.GetById(userId);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(responseDto);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userServiceMock.Setup(x => x.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserResponseDto?)null);

        // Act
        var result = await _controller.GetById(userId);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Create_ShouldReturnCreatedAtAction_WhenEmailIsUnique()
    {
        // Arrange
        var createDto = new CreateUserDto { Username = "newuser", Email = "new@test.com" };
        var responseDto = new UserResponseDto { Id = Guid.NewGuid(), Username = "newuser", Email = "new@test.com" };
        _userServiceMock.Setup(x => x.CreateUserAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult.Should().NotBeNull();
        createdResult!.StatusCode.Should().Be(201);
        createdResult.Value.Should().Be(responseDto);
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenEmailAlreadyExists()
    {
        // Arrange
        var createDto = new CreateUserDto { Username = "user", Email = "exists@test.com" };
        _userServiceMock.Setup(x => x.CreateUserAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("User with email 'exists@test.com' already exists."));

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var badRequest = result.Result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task GetUserDocuments_ShouldReturnPagedResult_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var pagedResult = new PagedResult<DocumentListItemDto>
        {
            Items = new List<DocumentListItemDto>(),
            PageNumber = 1,
            PageSize = 10,
            TotalCount = 0
        };
        _userServiceMock.Setup(x => x.GetUserDocumentsAsync(userId, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetUserDocuments(userId, 1, 10);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(pagedResult);
    }

    [Fact]
    public async Task GetUserDocuments_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userServiceMock.Setup(x => x.GetUserDocumentsAsync(userId, 1, 10, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException());

        // Act
        var result = await _controller.GetUserDocuments(userId, 1, 10);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }
}