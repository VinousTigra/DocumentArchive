using DocumentArchive.API.Controllers;
using DocumentArchive.Core.DTOs.ArchiveLog;
using DocumentArchive.Core.DTOs.Shared;
using DocumentArchive.Core.DTOs.Statistics;
using DocumentArchive.Core.Interfaces.Services;
using DocumentArchive.Core.Models;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DocumentArchive.Tests.ControllersTests;

public class ArchiveLogsControllerTests
{
    private readonly ArchiveLogsController _controller;
    private readonly Mock<IValidator<CreateArchiveLogDto>> _createValidatorMock;
    private readonly Mock<IArchiveLogService> _logServiceMock;

    public ArchiveLogsControllerTests()
    {
        _logServiceMock = new Mock<IArchiveLogService>();
        _createValidatorMock = new Mock<IValidator<CreateArchiveLogDto>>();
        _controller = new ArchiveLogsController(_logServiceMock.Object, _createValidatorMock.Object);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOk_WhenParametersValid()
    {
        // Arrange
        var pagedResult = new PagedResult<ArchiveLogListItemDto>();
        _logServiceMock.Setup(x => x.GetLogsAsync(
                1, 20, null, null, null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(pagedResult);
    }

    [Fact]
    public async Task GetAll_ShouldReturnBadRequest_WhenPageLessThan1()
    {
        // Act
        var result = await _controller.GetAll(0);

        // Assert
        var badRequest = result.Result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.StatusCode.Should().Be(400);
        badRequest.Value.Should().Be("Page must be >= 1.");
    }

    [Fact]
    public async Task GetAll_ShouldReturnBadRequest_WhenPageSizeOutOfRange()
    {
        // Act
        var result = await _controller.GetAll(pageSize: 101);

        // Assert
        var badRequest = result.Result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.StatusCode.Should().Be(400);
        badRequest.Value.Should().Be("Page size must be between 1 and 100.");
    }

    [Fact]
    public async Task GetAll_ShouldReturnBadRequest_WhenFromDateGreaterThanToDate()
    {
        // Act
        var result = await _controller.GetAll(
            fromDate: DateTime.UtcNow.AddDays(1),
            toDate: DateTime.UtcNow);

        // Assert
        var badRequest = result.Result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.StatusCode.Should().Be(400);
        badRequest.Value.Should().Be("fromDate cannot be later than toDate.");
    }

    [Fact]
    public async Task GetById_ShouldReturnOk_WhenLogExists()
    {
        // Arrange
        var logId = Guid.NewGuid();
        var responseDto = new ArchiveLogResponseDto { Id = logId, Action = "Test" };
        _logServiceMock.Setup(x => x.GetLogByIdAsync(logId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        // Act
        var result = await _controller.GetById(logId);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(responseDto);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenLogDoesNotExist()
    {
        // Arrange
        var logId = Guid.NewGuid();
        _logServiceMock.Setup(x => x.GetLogByIdAsync(logId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ArchiveLogResponseDto?)null);

        // Act
        var result = await _controller.GetById(logId);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Create_ShouldReturnCreated_WhenValid()
    {
        // Arrange
        var createDto = new CreateArchiveLogDto
        {
            Action = "Created",
            ActionType = ActionType.Created,
            DocumentId = Guid.NewGuid(),
            UserId = Guid.NewGuid()
        };
        var validationResult = new ValidationResult();
        _createValidatorMock.Setup(x => x.ValidateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        var responseDto = new ArchiveLogResponseDto { Id = Guid.NewGuid() };
        _logServiceMock.Setup(x => x.CreateLogAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult.Should().NotBeNull();
        createdResult!.StatusCode.Should().Be(201);
        createdResult.ActionName.Should().Be(nameof(_controller.GetById));
        createdResult.Value.Should().Be(responseDto);
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenValidationFails()
    {
        // Arrange
        var createDto = new CreateArchiveLogDto { Action = "" };
        var validationFailures = new List<ValidationFailure> { new("Action", "Action is required") };
        var validationResult = new ValidationResult(validationFailures);
        _createValidatorMock.Setup(x => x.ValidateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var badRequest = result.Result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.StatusCode.Should().Be(400);
        var errors = badRequest.Value as IEnumerable<ValidationFailure>;
        errors.Should().NotBeNull();
        errors!.Select(e => e.ErrorMessage).Should().Contain(new[] { "Action is required" });
    }

    [Fact]
    public async Task Create_ShouldThrowInvalidOperationException_WhenServiceThrows()
    {
        // Arrange
        var createDto = new CreateArchiveLogDto
        {
            Action = "Test",
            DocumentId = Guid.NewGuid(),
            UserId = Guid.NewGuid()
        };
        var validationResult = new ValidationResult();
        _createValidatorMock.Setup(x => x.ValidateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        _logServiceMock.Setup(x => x.CreateLogAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Document not found"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _controller.Create(createDto));
    }

    [Fact]
    public async Task Delete_ShouldReturnNoContent_WhenSuccess()
    {
        // Arrange
        var id = Guid.NewGuid();
        _logServiceMock.Setup(x => x.DeleteLogAsync(id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Delete(id);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_ShouldThrowKeyNotFoundException_WhenServiceThrows()
    {
        // Arrange
        var id = Guid.NewGuid();
        _logServiceMock.Setup(x => x.DeleteLogAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.Delete(id));
    }

    [Fact]
    public async Task GetLogsCountByActionType_ShouldReturnOk()
    {
        // Arrange
        var dict = new Dictionary<ActionType, int> { { ActionType.Created, 5 } };
        _logServiceMock.Setup(x => x.GetLogsCountByActionTypeAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dict);

        // Act
        var result = await _controller.GetLogsCountByActionType();

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().Be(dict);
    }

    [Fact]
    public async Task GetLogsStatistics_ShouldReturnOk()
    {
        // Arrange
        var stats = new LogsStatisticsDto();
        _logServiceMock.Setup(x => x.GetLogsStatisticsAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stats);

        // Act
        var result = await _controller.GetLogsStatistics();

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().Be(stats);
    }
}