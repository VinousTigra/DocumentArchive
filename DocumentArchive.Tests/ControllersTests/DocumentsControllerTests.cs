using System.Security.Claims;
using DocumentArchive.API.Controllers;
using DocumentArchive.Core.DTOs.ArchiveLog;
using DocumentArchive.Core.DTOs.Document;
using DocumentArchive.Core.DTOs.Shared;
using DocumentArchive.Core.DTOs.Statistics;
using DocumentArchive.Core.Interfaces.Services;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DocumentArchive.Tests.ControllersTests;

public class DocumentsControllerTests
{
    private readonly DocumentsController _controller;
    private readonly Mock<IDocumentService> _documentServiceMock;
    private readonly Mock<IValidator<CreateDocumentDto>> _createValidatorMock;
    private readonly Mock<IValidator<UpdateDocumentDto>> _updateValidatorMock;

    public DocumentsControllerTests()
    {
        _documentServiceMock = new Mock<IDocumentService>();
        _createValidatorMock = new Mock<IValidator<CreateDocumentDto>>();
        _updateValidatorMock = new Mock<IValidator<UpdateDocumentDto>>();
        _controller = new DocumentsController(
            _documentServiceMock.Object,
            _createValidatorMock.Object,
            _updateValidatorMock.Object);
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
    public async Task GetAll_ShouldReturnOk_WhenParametersValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissions = new List<string>();
        SetupUser(userId, permissions);

        var pagedResult = new PagedResult<DocumentListItemDto>();
        _documentServiceMock.Setup(x => x.GetDocumentsAsync(
                1, 10, null, null, null, null, null, null,
                userId, permissions, It.IsAny<CancellationToken>()))
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
        var result = await _controller.GetAll(page: 0);

        // Assert
        var badRequest = result.Result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.StatusCode.Should().Be(400);
        badRequest.Value.Should().Be("Page must be greater than or equal to 1.");
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
    public async Task GetAll_ShouldReturnBadRequest_WhenSortFormatInvalid()
    {
        // Act
        var result = await _controller.GetAll(sort: "invalid:format:extra");

        // Assert
        var badRequest = result.Result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.StatusCode.Should().Be(400);
        badRequest.Value.Should().Be("Invalid sort format. Expected format: field:direction,field:direction (e.g., title:asc,uploadDate:desc)");
    }

    [Fact]
    public async Task GetById_ShouldReturnOk_WhenDocumentExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissions = new List<string>();
        SetupUser(userId, permissions);
        var id = Guid.NewGuid();
        var documentDto = new DocumentResponseDto { Id = id };
        _documentServiceMock.Setup(x => x.GetDocumentByIdAsync(id, userId, permissions, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documentDto);

        // Act
        var result = await _controller.GetById(id);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(documentDto);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenDocumentDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissions = new List<string>();
        SetupUser(userId, permissions);
        var id = Guid.NewGuid();
        _documentServiceMock.Setup(x => x.GetDocumentByIdAsync(id, userId, permissions, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentResponseDto?)null);

        // Act
        var result = await _controller.GetById(id);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Create_ShouldReturnCreated_WhenValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupUser(userId, new List<string>());
        var createDto = new CreateDocumentDto { Title = "Doc", FileName = "doc.pdf" };
        var responseDto = new DocumentResponseDto { Id = Guid.NewGuid() };

        _createValidatorMock.Setup(x => x.ValidateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _documentServiceMock.Setup(x => x.CreateDocumentAsync(createDto, userId, It.IsAny<CancellationToken>()))
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
        var userId = Guid.NewGuid();
        SetupUser(userId, new List<string>());
        var createDto = new CreateDocumentDto { Title = "" };
        var failures = new List<ValidationFailure> { new("Title", "Title required") };
        var validationResult = new ValidationResult(failures);
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
        errors!.Select(e => e.ErrorMessage).Should().Contain("Title required");
    }

    [Fact]
    public async Task Create_ShouldThrowInvalidOperationException_WhenServiceThrows()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupUser(userId, new List<string>());
        var createDto = new CreateDocumentDto { Title = "Doc", FileName = "doc.pdf" };
        _createValidatorMock.Setup(x => x.ValidateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _documentServiceMock.Setup(x => x.CreateDocumentAsync(createDto, userId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Category not found"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _controller.Create(createDto));
    }

    [Fact]
    public async Task Update_ShouldReturnNoContent_WhenValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissions = new List<string> { "EditOwnDocuments" };
        SetupUser(userId, permissions);
        var id = Guid.NewGuid();
        var updateDto = new UpdateDocumentDto { Title = "Updated", FileName = "doc.pdf" };
        _updateValidatorMock.Setup(x => x.ValidateAsync(updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _documentServiceMock.Setup(x => x.UpdateDocumentAsync(id, updateDto, userId, permissions, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Update(id, updateDto);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Update_ShouldThrowUnauthorizedAccessException_WhenServiceThrows()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissions = new List<string>();
        SetupUser(userId, permissions);
        var id = Guid.NewGuid();
        var updateDto = new UpdateDocumentDto { Title = "Updated", FileName = "doc.pdf" };
        _updateValidatorMock.Setup(x => x.ValidateAsync(updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _documentServiceMock.Setup(x => x.UpdateDocumentAsync(id, updateDto, userId, permissions, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.Update(id, updateDto));
    }

    [Fact]
    public async Task Delete_ShouldReturnNoContent_WhenSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissions = new List<string> { "DeleteOwnDocuments" };
        SetupUser(userId, permissions);
        var id = Guid.NewGuid();
        _documentServiceMock.Setup(x => x.DeleteDocumentAsync(id, userId, permissions, It.IsAny<CancellationToken>()))
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
        var userId = Guid.NewGuid();
        var permissions = new List<string> { "DeleteOwnDocuments" };
        SetupUser(userId, permissions);
        var id = Guid.NewGuid();
        _documentServiceMock.Setup(x => x.DeleteDocumentAsync(id, userId, permissions, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.Delete(id));
    }

    [Fact]
    public async Task GetDocumentLogs_ShouldReturnOk_WhenDocumentExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissions = new List<string>();
        SetupUser(userId, permissions);
        var id = Guid.NewGuid();
        var pagedResult = new PagedResult<ArchiveLogListItemDto>();
        _documentServiceMock.Setup(x => x.GetDocumentLogsAsync(id, 1, 20, userId, permissions, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetDocumentLogs(id);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(pagedResult);
    }

    [Fact]
    public async Task GetDocumentLogs_ShouldReturnBadRequest_WhenPageInvalid()
    {
        // Act
        var result = await _controller.GetDocumentLogs(Guid.NewGuid(), page: 0);

        // Assert
        var badRequest = result.Result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.StatusCode.Should().Be(400);
        badRequest.Value.Should().Be("Page must be >= 1.");
    }

    [Fact]
    public async Task CreateBulk_ShouldReturnOk_WhenValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissions = new List<string>();
        SetupUser(userId, permissions);
        var dtos = new List<CreateDocumentDto> { new() { Title = "Doc1", FileName = "1.pdf" } };
        var bulkResult = new BulkOperationResult<Guid>();
        _documentServiceMock.Setup(x => x.CreateBulkAsync(dtos, userId, permissions, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        // Act
        var result = await _controller.CreateBulk(dtos);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(bulkResult);
    }

    [Fact]
    public async Task CreateBulk_ShouldReturnBadRequest_WhenListEmpty()
    {
        // Act
        var result = await _controller.CreateBulk(new List<CreateDocumentDto>());

        // Assert
        var badRequest = result.Result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.StatusCode.Should().Be(400);
        badRequest.Value.Should().Be("Bulk request cannot be empty.");
    }

    [Fact]
    public async Task CreateBulk_ShouldReturnBadRequest_WhenTooManyItems()
    {
        // Arrange
        var dtos = new List<CreateDocumentDto>();
        for (int i = 0; i < 101; i++)
            dtos.Add(new CreateDocumentDto());

        // Act
        var result = await _controller.CreateBulk(dtos);

        // Assert
        var badRequest = result.Result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.StatusCode.Should().Be(400);
        badRequest.Value.Should().Be("Too many items in bulk request. Maximum allowed: 100.");
    }

    // Аналогичные тесты для UpdateBulk и DeleteBulk можно добавить по аналогии

    [Fact]
    public async Task GetDocumentsCountByCategory_ShouldReturnOk()
    {
        // Arrange
        var dict = new Dictionary<string, int> { { "Cat", 5 } };
        _documentServiceMock.Setup(x => x.GetDocumentsCountByCategoryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(dict);

        // Act
        var result = await _controller.GetDocumentsCountByCategory(default);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().Be(dict);
    }

    [Fact]
    public async Task GetDocumentsStatistics_ShouldReturnOk()
    {
        // Arrange
        var stats = new DocumentsStatisticsDto();
        _documentServiceMock.Setup(x => x.GetDocumentsStatisticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(stats);

        // Act
        var result = await _controller.GetDocumentsStatistics(default);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().Be(stats);
    }
}