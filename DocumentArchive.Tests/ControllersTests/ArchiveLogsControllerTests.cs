using AutoMapper;
using DocumentArchive.API.Controllers;
using DocumentArchive.Core.DTOs.ArchiveLog;
using DocumentArchive.Core.Models;
using DocumentArchive.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DocumentArchive.Tests.ControllersTests;

public class ArchiveLogsControllerTests
{
    private readonly Mock<ArchiveLogRepository> _logRepoMock;
    private readonly Mock<DocumentRepository> _documentRepoMock;
    private readonly Mock<UserRepository> _userRepoMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly ArchiveLogsController _controller;

    public ArchiveLogsControllerTests()
    {
        _logRepoMock = new Mock<ArchiveLogRepository>();
        _documentRepoMock = new Mock<DocumentRepository>();
        _userRepoMock = new Mock<UserRepository>();
        _mapperMock = new Mock<IMapper>();
        _controller = new ArchiveLogsController(
            _logRepoMock.Object,
            _documentRepoMock.Object,
            _userRepoMock.Object,
            _mapperMock.Object);
    }

    [Fact]
    public async Task GetById_ShouldReturnOk_WhenLogExists()
    {
        // Arrange
        var logId = Guid.NewGuid();
        var log = new ArchiveLog { Id = logId, Action = "Test" };
        var responseDto = new ArchiveLogResponseDto { Id = logId, Action = "Test" };
        _logRepoMock.Setup(x => x.GetByIdAsync(logId)).ReturnsAsync(log);
        _mapperMock.Setup(x => x.Map<ArchiveLogResponseDto>(log)).Returns(responseDto);

        // Act
        var result = await _controller.GetById(logId);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
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
        _documentRepoMock.Setup(x => x.GetByIdAsync(createDto.DocumentId))
            .ReturnsAsync(new Document());
        _userRepoMock.Setup(x => x.GetByIdAsync(createDto.UserId))
            .ReturnsAsync(new User());
        var log = new ArchiveLog { Id = Guid.NewGuid() };
        var responseDto = new ArchiveLogResponseDto { Id = log.Id };
        _mapperMock.Setup(x => x.Map<ArchiveLog>(createDto)).Returns(log);
        _mapperMock.Setup(x => x.Map<ArchiveLogResponseDto>(log)).Returns(responseDto);

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult.Should().NotBeNull();
        createdResult!.StatusCode.Should().Be(201);
    }
}