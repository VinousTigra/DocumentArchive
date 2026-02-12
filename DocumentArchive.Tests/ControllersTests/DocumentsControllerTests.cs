using AutoMapper;
using DocumentArchive.API.Controllers;
using DocumentArchive.Core.DTOs.Document;
using DocumentArchive.Core.Models;
using DocumentArchive.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DocumentArchive.Tests.ControllersTests;

public class DocumentsControllerTests
{
    private readonly Mock<DocumentRepository> _documentRepoMock;
    private readonly Mock<CategoryRepository> _categoryRepoMock;
    private readonly Mock<UserRepository> _userRepoMock;
    private readonly Mock<ArchiveLogRepository> _logRepoMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly DocumentsController _controller;

    public DocumentsControllerTests()
    {
        _documentRepoMock = new Mock<DocumentRepository>();
        _categoryRepoMock = new Mock<CategoryRepository>();
        _userRepoMock = new Mock<UserRepository>();
        _logRepoMock = new Mock<ArchiveLogRepository>();
        _mapperMock = new Mock<IMapper>();
        _controller = new DocumentsController(
            _documentRepoMock.Object,
            _categoryRepoMock.Object,
            _userRepoMock.Object,
            _logRepoMock.Object,
            _mapperMock.Object);
    }

    [Fact]
    public async Task GetById_ShouldReturnOk_WhenDocumentExists()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var document = new Document { Id = documentId, Title = "Test" };
        var responseDto = new DocumentResponseDto { Id = documentId, Title = "Test" };
        _documentRepoMock.Setup(x => x.GetByIdAsync(documentId))
            .ReturnsAsync(document);
        _mapperMock.Setup(x => x.Map<DocumentResponseDto>(document))
            .Returns(responseDto);

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
        _documentRepoMock.Setup(x => x.GetByIdAsync(documentId))
            .ReturnsAsync((Document?)null);

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
        var category = new Category { Id = createDto.CategoryId.Value };
        var user = new User { Id = createDto.UserId.Value };
        var document = new Document { Id = Guid.NewGuid(), Title = "New Doc" };
        var responseDto = new DocumentResponseDto { Id = document.Id, Title = "New Doc" };

        _categoryRepoMock.Setup(x => x.GetByIdAsync(createDto.CategoryId.Value))
            .ReturnsAsync(category);
        _userRepoMock.Setup(x => x.GetByIdAsync(createDto.UserId.Value))
            .ReturnsAsync(user);
        _mapperMock.Setup(x => x.Map<Document>(createDto)).Returns(document);
        _mapperMock.Setup(x => x.Map<DocumentResponseDto>(document)).Returns(responseDto);
        _documentRepoMock.Setup(x => x.AddAsync(document)).Returns(Task.CompletedTask);

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
    public async Task Create_ShouldReturnBadRequest_WhenCategoryNotFound()
    {
        // Arrange
        var createDto = new CreateDocumentDto
        {
            Title = "Doc",
            FileName = "doc.pdf",
            CategoryId = Guid.NewGuid()
        };
        _categoryRepoMock.Setup(x => x.GetByIdAsync(createDto.CategoryId.Value))
            .ReturnsAsync((Category?)null);

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var badRequest = result.Result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Delete_ShouldReturnNoContent_WhenDocumentExists()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var document = new Document { Id = documentId };
        _documentRepoMock.Setup(x => x.GetByIdAsync(documentId))
            .ReturnsAsync(document);
        _documentRepoMock.Setup(x => x.DeleteAsync(documentId))
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
        _documentRepoMock.Setup(x => x.GetByIdAsync(documentId))
            .ReturnsAsync((Document?)null);

        // Act
        var result = await _controller.Delete(documentId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task CreateBulk_ShouldReturnOkWithResults()
    {
        // Arrange
        var createDtos = new List<CreateDocumentDto>
        {
            new() { Title = "Doc1", FileName = "1.pdf" },
            new() { Title = "Doc2", FileName = "2.pdf" }
        };
        _mapperMock.Setup(x => x.Map<Document>(It.IsAny<CreateDocumentDto>()))
            .Returns((CreateDocumentDto dto) => new Document { Id = Guid.NewGuid(), Title = dto.Title });
        _documentRepoMock.Setup(x => x.AddAsync(It.IsAny<Document>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.CreateBulk(createDtos);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }
}