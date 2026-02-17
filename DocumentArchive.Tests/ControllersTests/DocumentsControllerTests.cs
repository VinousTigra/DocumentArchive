using AutoMapper;
using DocumentArchive.API.Controllers;
using DocumentArchive.Core.DTOs.ArchiveLog;
using DocumentArchive.Core.DTOs.Document;
using DocumentArchive.Core.DTOs.Shared;
using DocumentArchive.Core.Interfaces.Services;
using DocumentArchive.Core.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace DocumentArchive.Tests.ControllersTests;

public class DocumentsControllerTests
{
    private readonly Mock<IDocumentService> _documentServiceMock;
    private readonly Mock<ILogger<DocumentsController>> _loggerMock;
    private readonly DocumentsController _controller;

    public DocumentsControllerTests()
    {
        _documentServiceMock = new Mock<IDocumentService>();
        _loggerMock = new Mock<ILogger<DocumentsController>>();
        _controller = new DocumentsController(_documentServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetById_ShouldReturnOk_WhenDocumentExists()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var responseDto = new DocumentResponseDto { Id = documentId, Title = "Test" };
        _documentServiceMock.Setup(x => x.GetDocumentByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        // Act
        var result = await _controller.GetById(documentId);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(responseDto);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenDocumentDoesNotExist()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        _documentServiceMock.Setup(x => x.GetDocumentByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentResponseDto?)null);

        // Act
        var result = await _controller.GetById(documentId);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Create_ShouldReturnCreatedAtAction_WhenValid()
    {
        // Arrange
        var createDto = new CreateDocumentDto
        {
            Title = "New Doc",
            FileName = "doc.pdf",
            CategoryId = Guid.NewGuid(),
            UserId = Guid.NewGuid()
        };
        var responseDto = new DocumentResponseDto { Id = Guid.NewGuid(), Title = "New Doc" };
        _documentServiceMock.Setup(x => x.CreateDocumentAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult.Should().NotBeNull();
        createdResult!.StatusCode.Should().Be(201);
        createdResult.Value.Should().Be(responseDto);
        createdResult.ActionName.Should().Be(nameof(_controller.GetById));
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenBusinessRuleViolation()
    {
        // Arrange
        var createDto = new CreateDocumentDto { Title = "Doc", FileName = "doc.pdf" };
        _documentServiceMock.Setup(x => x.CreateDocumentAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Business rule error"));

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var badRequest = result.Result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.StatusCode.Should().Be(400);
        badRequest.Value.Should().Be("Operation cannot be completed due to business rule violation.");
    }

    [Fact]
    public async Task Delete_ShouldReturnNoContent_WhenDocumentExists()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        _documentServiceMock.Setup(x => x.DeleteDocumentAsync(documentId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Delete(documentId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenDocumentDoesNotExist()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        _documentServiceMock.Setup(x => x.DeleteDocumentAsync(documentId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException());

        // Act
        var result = await _controller.Delete(documentId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetDocumentLogs_ShouldReturnPagedResult_WhenDocumentExists()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var pagedResult = new PagedResult<ArchiveLogListItemDto>
        {
            Items = new List<ArchiveLogListItemDto>(),
            PageNumber = 1,
            PageSize = 20,
            TotalCount = 0
        };
        _documentServiceMock.Setup(x => x.GetDocumentLogsAsync(documentId, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetDocumentLogs(documentId, 1, 20);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(pagedResult);
    }

    [Fact]
    public async Task GetDocumentLogs_ShouldReturnNotFound_WhenDocumentDoesNotExist()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        _documentServiceMock.Setup(x => x.GetDocumentLogsAsync(documentId, 1, 20, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException());

        // Act
        var result = await _controller.GetDocumentLogs(documentId, 1, 20);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task CreateBulk_ShouldReturnOk_WhenRequestIsValid()
    {
        // Arrange
        var createDtos = new List<CreateDocumentDto> { new() { Title = "Doc1", FileName = "1.pdf" } };
        var bulkResult = new BulkOperationResult<Guid>();
        _documentServiceMock.Setup(x => x.CreateBulkAsync(createDtos, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        // Act
        var result = await _controller.CreateBulk(createDtos);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task CreateBulk_ShouldReturnBadRequest_WhenListExceedsMaxSize()
    {
        // Arrange
        var createDtos = new List<CreateDocumentDto>();
        for (int i = 0; i < 101; i++)
            createDtos.Add(new CreateDocumentDto());

        // Act
        var result = await _controller.CreateBulk(createDtos);

        // Assert
        var badRequest = result.Result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task DeleteBulk_ShouldReturnOk_WhenIdsAreValid()
    {
        // Arrange
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var bulkResult = new BulkOperationResult<Guid>();
        _documentServiceMock.Setup(x => x.DeleteBulkAsync(ids, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        // Act
        var result = await _controller.DeleteBulk(ids);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task DeleteBulk_ShouldReturnBadRequest_WhenIdsEmpty()
    {
        // Arrange
        var ids = Array.Empty<Guid>();

        // Act
        var result = await _controller.DeleteBulk(ids);

        // Assert
        var badRequest = result.Result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.StatusCode.Should().Be(400);
    }
}