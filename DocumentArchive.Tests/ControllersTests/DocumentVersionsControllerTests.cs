using System.Security.Claims;
using DocumentArchive.API.Controllers;
using DocumentArchive.Core.DTOs.DocumentVersion;
using DocumentArchive.Core.Interfaces.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DocumentArchive.Tests.ControllersTests;

public class DocumentVersionsControllerTests
{
    private readonly DocumentVersionsController _controller;
    private readonly Mock<IDocumentVersionService> _versionServiceMock;

    public DocumentVersionsControllerTests()
    {
        _versionServiceMock = new Mock<IDocumentVersionService>();
        _controller = new DocumentVersionsController(_versionServiceMock.Object);
    }

    private void SetupUser(Guid userId, List<string> permissions)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        claims.AddRange(permissions.Select(p => new Claim("permission", p)));

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    [Fact]
    public async Task GetAll_ShouldReturnOk_WithVersions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissions = new List<string>();
        SetupUser(userId, permissions);

        var versions = new List<DocumentVersionListItemDto> { new() { Id = Guid.NewGuid() } };
        _versionServiceMock.Setup(x => x.GetAllAsync(null, userId, permissions, It.IsAny<CancellationToken>()))
            .ReturnsAsync(versions);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(versions);
    }

    [Fact]
    public async Task GetAll_WithDocumentId_ShouldCallServiceWithDocumentId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissions = new List<string>();
        SetupUser(userId, permissions);
        var documentId = Guid.NewGuid();
        var versions = new List<DocumentVersionListItemDto>();
        _versionServiceMock.Setup(x => x.GetAllAsync(documentId, userId, permissions, It.IsAny<CancellationToken>()))
            .ReturnsAsync(versions);

        // Act
        var result = await _controller.GetAll(documentId);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(versions);
        _versionServiceMock.Verify(x => x.GetAllAsync(documentId, userId, permissions, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_ShouldReturnOk_WhenVersionExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissions = new List<string>();
        SetupUser(userId, permissions);
        var versionId = Guid.NewGuid();
        var versionDto = new DocumentVersionResponseDto { Id = versionId };
        _versionServiceMock.Setup(x => x.GetByIdAsync(versionId, userId, permissions, It.IsAny<CancellationToken>()))
            .ReturnsAsync(versionDto);

        // Act
        var result = await _controller.GetById(versionId, default);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(versionDto);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenVersionDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissions = new List<string>();
        SetupUser(userId, permissions);
        var versionId = Guid.NewGuid();
        _versionServiceMock.Setup(x => x.GetByIdAsync(versionId, userId, permissions, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentVersionResponseDto?)null);

        // Act
        var result = await _controller.GetById(versionId, default);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Create_ShouldReturnCreated_WhenValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissions = new List<string> { "EditOwnDocuments" };
        SetupUser(userId, permissions);
        var createDto = new CreateDocumentVersionDto { DocumentId = Guid.NewGuid(), VersionNumber = 1, FileName = "v1.pdf", FileSize = 1024 };
        var responseDto = new DocumentVersionResponseDto { Id = Guid.NewGuid() };
        _versionServiceMock.Setup(x => x.CreateAsync(createDto, userId, permissions, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        // Act
        var result = await _controller.Create(createDto, default);

        // Assert
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult.Should().NotBeNull();
        createdResult!.StatusCode.Should().Be(201);
        createdResult.ActionName.Should().Be(nameof(_controller.GetById));
        createdResult.Value.Should().Be(responseDto);
    }

    [Fact]
    public async Task Update_ShouldReturnNoContent_WhenValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissions = new List<string> { "EditOwnDocuments" };
        SetupUser(userId, permissions);
        var id = Guid.NewGuid();
        var updateDto = new UpdateDocumentVersionDto { Comment = "Updated" };
        _versionServiceMock.Setup(x => x.UpdateAsync(id, updateDto, userId, permissions, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Update(id, updateDto, default);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Update_ShouldThrowKeyNotFoundException_WhenServiceThrows()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissions = new List<string> { "EditOwnDocuments" };
        SetupUser(userId, permissions);
        var id = Guid.NewGuid();
        var updateDto = new UpdateDocumentVersionDto { Comment = "Updated" };
        _versionServiceMock.Setup(x => x.UpdateAsync(id, updateDto, userId, permissions, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.Update(id, updateDto, default));
    }

    [Fact]
    public async Task Delete_ShouldReturnNoContent_WhenSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissions = new List<string> { "DeleteOwnDocuments" };
        SetupUser(userId, permissions);
        var id = Guid.NewGuid();
        _versionServiceMock.Setup(x => x.DeleteAsync(id, userId, permissions, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Delete(id, default);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_ShouldThrowKeyNotFoundException_WhenServiceThrows()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissions = new List<string> { "DeleteOwnDocuments" };
        SetupUser(userId, permissions);
        var id = Guid.NewGuid();
        _versionServiceMock.Setup(x => x.DeleteAsync(id, userId, permissions, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.Delete(id, default));
    }
}