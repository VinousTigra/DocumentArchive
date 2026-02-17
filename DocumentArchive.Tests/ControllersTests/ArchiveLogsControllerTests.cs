using AutoMapper;
using DocumentArchive.API.Controllers;
using DocumentArchive.Core.DTOs.ArchiveLog;
using DocumentArchive.Core.DTOs.Shared;
using DocumentArchive.Core.Interfaces.Services;
using DocumentArchive.Core.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace DocumentArchive.Tests.ControllersTests;

public class ArchiveLogsControllerTests
{
    private readonly Mock<IArchiveLogService> _logServiceMock;
    private readonly Mock<ILogger<ArchiveLogsController>> _loggerMock;
    private readonly ArchiveLogsController _controller;

    public ArchiveLogsControllerTests()
    {
        _logServiceMock = new Mock<IArchiveLogService>();
        _loggerMock = new Mock<ILogger<ArchiveLogsController>>();
        _controller = new ArchiveLogsController(_logServiceMock.Object, _loggerMock.Object);
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
    public async Task Create_ShouldReturnCreatedAtAction_WhenValid()
    {
        // Arrange
        var createDto = new CreateArchiveLogDto
        {
            Action = "Created",
            ActionType = ActionType.Created,
            DocumentId = Guid.NewGuid(),
            UserId = Guid.NewGuid()
        };
        var responseDto = new ArchiveLogResponseDto { Id = Guid.NewGuid() };
        _logServiceMock.Setup(x => x.CreateLogAsync(createDto, It.IsAny<CancellationToken>()))
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
    public async Task Create_ShouldReturnBadRequest_WhenDocumentNotFound()
    {
        // Arrange
        var createDto = new CreateArchiveLogDto { DocumentId = Guid.NewGuid() };
        _logServiceMock.Setup(x => x.CreateLogAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Document not found"));

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var badRequest = result.Result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Delete_ShouldReturnNoContent_WhenLogExists()
    {
        // Arrange
        var logId = Guid.NewGuid();
        _logServiceMock.Setup(x => x.DeleteLogAsync(logId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Delete(logId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenLogDoesNotExist()
    {
        // Arrange
        var logId = Guid.NewGuid();
        _logServiceMock.Setup(x => x.DeleteLogAsync(logId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException());

        // Act
        var result = await _controller.Delete(logId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }
}