#nullable disable
using DocumentArchive.API.Controllers;
using DocumentArchive.Core.DTOs.Document;
using DocumentArchive.Core.DTOs.Shared;
using DocumentArchive.Core.DTOs.User;
using DocumentArchive.Core.Interfaces.Services;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace DocumentArchive.Tests.ControllersTests;

public class UsersControllerTests
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<IValidator<CreateUserDto>> _createValidatorMock;
    private readonly Mock<IValidator<UpdateUserDto>> _updateValidatorMock;
    private readonly Mock<ILogger<UsersController>> _loggerMock;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _userServiceMock = new Mock<IUserService>();
        _createValidatorMock = new Mock<IValidator<CreateUserDto>>();
        _updateValidatorMock = new Mock<IValidator<UpdateUserDto>>();
        _loggerMock = new Mock<ILogger<UsersController>>();
        _controller = new UsersController(
            _userServiceMock.Object,
            _createValidatorMock.Object,
            _updateValidatorMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOk_WhenParametersValid()
    {
        var pagedResult = new PagedResult<UserListItemDto>();
        _userServiceMock.Setup(x => x.GetUsersAsync(1, 10, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await _controller.GetAll();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(pagedResult);
    }

    [Fact]
    public async Task GetAll_ShouldReturnBadRequest_WhenPageLessThan1()
    {
        var result = await _controller.GetAll(page: 0);
        var badRequest = result.Result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.StatusCode.Should().Be(400);
        badRequest.Value.Should().Be("Page must be >= 1.");
    }

    [Fact]
    public async Task GetAll_ShouldReturnBadRequest_WhenPageSizeOutOfRange()
    {
        var result = await _controller.GetAll(pageSize: 101);
        var badRequest = result.Result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.StatusCode.Should().Be(400);
        badRequest.Value.Should().Be("Page size must be between 1 and 100.");
    }

    [Fact]
    public async Task GetById_ShouldReturnOk_WhenUserExists()
    {
        var userId = Guid.NewGuid();
        var responseDto = new UserResponseDto { Id = userId, Username = "test" };
        _userServiceMock.Setup(x => x.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        var result = await _controller.GetById(userId);
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(responseDto);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        var userId = Guid.NewGuid();
        _userServiceMock.Setup(x => x.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserResponseDto?)null);

        var result = await _controller.GetById(userId);
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Create_ShouldReturnCreated_WhenValid()
    {
        var createDto = new CreateUserDto { Username = "newuser", Email = "new@test.com" };
        var validationResult = new ValidationResult();
        _createValidatorMock.Setup(x => x.ValidateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        var responseDto = new UserResponseDto { Id = Guid.NewGuid(), Username = "newuser", Email = "new@test.com" };
        _userServiceMock.Setup(x => x.CreateUserAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        var result = await _controller.Create(createDto);
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult.Should().NotBeNull();
        createdResult!.StatusCode.Should().Be(201);
        createdResult.ActionName.Should().Be(nameof(_controller.GetById));
        createdResult.Value.Should().Be(responseDto);
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenValidationFails()
    {
        var createDto = new CreateUserDto { Username = "", Email = "invalid" };
        var validationFailures = new List<ValidationFailure>
        {
            new("Username", "Username is required"),
            new("Email", "Invalid email format")
        };
        var validationResult = new ValidationResult(validationFailures);
        _createValidatorMock.Setup(x => x.ValidateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        var result = await _controller.Create(createDto);
        var badRequest = result.Result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.StatusCode.Should().Be(400);
        var errors = badRequest.Value as IEnumerable<ValidationFailure>;
        errors.Should().NotBeNull();
        errors!.Select(e => e.ErrorMessage).Should().Contain(new[] { "Username is required", "Invalid email format" });
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenInvalidOperationException()
    {
        var createDto = new CreateUserDto { Username = "user", Email = "exists@test.com" };
        var validationResult = new ValidationResult();
        _createValidatorMock.Setup(x => x.ValidateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        _userServiceMock.Setup(x => x.CreateUserAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("User with email already exists."));

        var result = await _controller.Create(createDto);
        var badRequest = result.Result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.StatusCode.Should().Be(400);
        badRequest.Value.Should().Be("Operation cannot be completed due to business rule violation.");
    }

    [Fact]
    public async Task Update_ShouldReturnNoContent_WhenValid()
    {
        var id = Guid.NewGuid();
        var updateDto = new UpdateUserDto { Username = "updated" };
        var validationResult = new ValidationResult();
        _updateValidatorMock.Setup(x => x.ValidateAsync(updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        _userServiceMock.Setup(x => x.UpdateUserAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.Update(id, updateDto);
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Update_ShouldReturnNotFound_WhenKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        var updateDto = new UpdateUserDto { Username = "updated" };
        var validationResult = new ValidationResult();
        _updateValidatorMock.Setup(x => x.ValidateAsync(updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        _userServiceMock.Setup(x => x.UpdateUserAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException());

        var result = await _controller.Update(id, updateDto);
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_ShouldReturnNoContent_WhenSuccess()
    {
        var id = Guid.NewGuid();
        _userServiceMock.Setup(x => x.DeleteUserAsync(id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.Delete(id);
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        _userServiceMock.Setup(x => x.DeleteUserAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException());

        var result = await _controller.Delete(id);
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_ShouldReturnBadRequest_WhenInvalidOperationException()
    {
        var id = Guid.NewGuid();
        _userServiceMock.Setup(x => x.DeleteUserAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cannot delete user with documents."));

        var result = await _controller.Delete(id);
        var badRequest = result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.StatusCode.Should().Be(400);
        badRequest.Value.Should().Be("Operation cannot be completed due to business rule violation.");
    }

    [Fact]
    public async Task GetUserDocuments_ShouldReturnOk_WhenUserExists()
    {
        var userId = Guid.NewGuid();
        var pagedResult = new PagedResult<DocumentListItemDto>();
        _userServiceMock.Setup(x => x.GetUserDocumentsAsync(userId, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await _controller.GetUserDocuments(userId);
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(pagedResult);
    }

    [Fact]
    public async Task GetUserDocuments_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        var userId = Guid.NewGuid();
        _userServiceMock.Setup(x => x.GetUserDocumentsAsync(userId, 1, 10, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException());

        var result = await _controller.GetUserDocuments(userId);
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetUserDocuments_ShouldReturnBadRequest_WhenPageInvalid()
    {
        var result = await _controller.GetUserDocuments(Guid.NewGuid(), page: 0);
        var badRequest = result.Result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.StatusCode.Should().Be(400);
        badRequest.Value.Should().Be("Page must be >= 1.");
    }
}