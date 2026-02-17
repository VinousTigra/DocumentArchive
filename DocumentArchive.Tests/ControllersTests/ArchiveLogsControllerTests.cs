using DocumentArchive.API.Controllers;
using DocumentArchive.Core.DTOs.ArchiveLog;
using DocumentArchive.Core.DTOs.Shared;
using DocumentArchive.Core.Interfaces.Services;
using DocumentArchive.Core.Models;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace DocumentArchive.Tests.ControllersTests;

public class ArchiveLogsControllerTests
{
    private readonly Mock<IArchiveLogService> _logServiceMock;
    private readonly Mock<IValidator<CreateArchiveLogDto>> _createValidatorMock;
    private readonly Mock<ILogger<ArchiveLogsController>> _loggerMock;
    private readonly ArchiveLogsController _controller;

    public ArchiveLogsControllerTests()
    {
        _logServiceMock = new Mock<IArchiveLogService>();
        _createValidatorMock = new Mock<IValidator<CreateArchiveLogDto>>();
        _loggerMock = new Mock<ILogger<ArchiveLogsController>>();
        _controller = new ArchiveLogsController(
            _logServiceMock.Object,
            _createValidatorMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOk_WhenParametersValid()
    {
        var pagedResult = new PagedResult<ArchiveLogListItemDto>();
        _logServiceMock.Setup(x => x.GetLogsAsync(
                1, 20, null, null, null, null, null, null, It.IsAny<CancellationToken>()))
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
    public async Task GetAll_ShouldReturnBadRequest_WhenFromDateGreaterThanToDate()
    {
        var result = await _controller.GetAll(
            fromDate: DateTime.UtcNow.AddDays(1),
            toDate: DateTime.UtcNow);
        var badRequest = result.Result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.StatusCode.Should().Be(400);
        badRequest.Value.Should().Be("fromDate cannot be later than toDate.");
    }

    [Fact]
    public async Task GetById_ShouldReturnOk_WhenLogExists()
    {
        var logId = Guid.NewGuid();
        var responseDto = new ArchiveLogResponseDto { Id = logId, Action = "Test" };
        _logServiceMock.Setup(x => x.GetLogByIdAsync(logId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        var result = await _controller.GetById(logId);
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(responseDto);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenLogDoesNotExist()
    {
        var logId = Guid.NewGuid();
        _logServiceMock.Setup(x => x.GetLogByIdAsync(logId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ArchiveLogResponseDto?)null);

        var result = await _controller.GetById(logId);
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Create_ShouldReturnCreated_WhenValid()
    {
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
        var createDto = new CreateArchiveLogDto { Action = "" };
        var validationFailures = new List<ValidationFailure>
        {
            new("Action", "Action is required")
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
        errors!.Select(e => e.ErrorMessage).Should().Contain(new[] { "Action is required" });
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenInvalidOperationException()
    {
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

        var result = await _controller.Create(createDto);
        var badRequest = result.Result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.StatusCode.Should().Be(400);
        badRequest.Value.Should().Be("Operation cannot be completed due to business rule violation.");
    }

    [Fact]
    public async Task Delete_ShouldReturnNoContent_WhenSuccess()
    {
        var id = Guid.NewGuid();
        _logServiceMock.Setup(x => x.DeleteLogAsync(id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.Delete(id);
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        _logServiceMock.Setup(x => x.DeleteLogAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException());

        var result = await _controller.Delete(id);
        result.Should().BeOfType<NotFoundResult>();
    }
}