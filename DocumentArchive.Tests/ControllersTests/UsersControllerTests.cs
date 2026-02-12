using AutoMapper;
using DocumentArchive.API.Controllers;
using DocumentArchive.Core.DTOs.User;
using DocumentArchive.Core.Models;
using DocumentArchive.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DocumentArchive.Tests.ControllersTests;

public class UsersControllerTests
{
    private readonly Mock<UserRepository> _userRepoMock;
    private readonly Mock<DocumentRepository> _documentRepoMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _userRepoMock = new Mock<UserRepository>();
        _documentRepoMock = new Mock<DocumentRepository>();
        _mapperMock = new Mock<IMapper>();
        _controller = new UsersController(
            _userRepoMock.Object,
            _documentRepoMock.Object,
            _mapperMock.Object);
    }

    [Fact]
    public async Task GetById_ShouldReturnOk_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Username = "test" };
        var responseDto = new UserResponseDto { Id = userId, Username = "test" };
        _userRepoMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _mapperMock.Setup(x => x.Map<UserResponseDto>(user)).Returns(responseDto);

        // Act
        var result = await _controller.GetById(userId);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(responseDto);
    }

    [Fact]
    public async Task Create_ShouldReturnCreatedAtAction_WhenEmailIsUnique()
    {
        // Arrange
        var createDto = new CreateUserDto { Username = "newuser", Email = "new@test.com" };
        _userRepoMock.Setup(x => x.FindByEmailAsync(createDto.Email))
            .ReturnsAsync((User?)null);
        var user = new User { Id = Guid.NewGuid(), Username = "newuser", Email = "new@test.com" };
        var responseDto = new UserResponseDto { Id = user.Id, Username = "newuser", Email = "new@test.com" };
        _mapperMock.Setup(x => x.Map<User>(createDto)).Returns(user);
        _mapperMock.Setup(x => x.Map<UserResponseDto>(user)).Returns(responseDto);

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult.Should().NotBeNull();
        createdResult!.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenEmailAlreadyExists()
    {
        // Arrange
        var createDto = new CreateUserDto { Username = "user", Email = "exists@test.com" };
        _userRepoMock.Setup(x => x.FindByEmailAsync(createDto.Email))
            .ReturnsAsync(new User());

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }
}