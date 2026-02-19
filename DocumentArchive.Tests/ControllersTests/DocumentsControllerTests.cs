using DocumentArchive.API.Controllers;
using DocumentArchive.Core.DTOs.ArchiveLog;
using DocumentArchive.Core.DTOs.Document;
using DocumentArchive.Core.DTOs.Shared;
using DocumentArchive.Core.DTOs.Statistics;
using DocumentArchive.Core.Interfaces.Services;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DocumentArchive.Tests.ControllersTests;

public class DocumentsControllerTests
{
    private readonly DocumentsController _controller;
    private readonly Mock<IValidator<CreateDocumentDto>> _createValidatorMock;
    private readonly Mock<IDocumentService> _documentServiceMock;
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

    [Fact]
    public async Task GetAll_ShouldReturnOk_WhenParametersValid()
    {
        var pagedResult = new PagedResult<DocumentListItemDto>();
        _documentServiceMock.Setup(x => x.GetDocumentsAsync(
                1, 10, null, null, null, null, null, null, It.IsAny<CancellationToken>()))
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
        var result = await _controller.GetAll(0);
        var badRequest = result.Result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.StatusCode.Should().Be(400);
        badRequest.Value.Should().Be("Page must be greater than or equal to 1.");
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
    public async Task GetAll_ShouldReturnBadRequest_WhenSortFormatInvalid()
    {
        var result = await _controller.GetAll(sort: "invalid:format:extra");
        var badRequest = result.Result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.StatusCode.Should().Be(400);
        badRequest.Value.Should()
            .Be(
                "Invalid sort format. Expected format: field:direction,field:direction (e.g., title:asc,uploadDate:desc)");
    }

    [Fact]
    public async Task GetById_ShouldReturnOk_WhenDocumentExists()
    {
        var documentId = Guid.NewGuid();
        var responseDto = new DocumentResponseDto { Id = documentId, Title = "Test" };
        _documentServiceMock.Setup(x => x.GetDocumentByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        var result = await _controller.GetById(documentId);
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(responseDto);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenDocumentDoesNotExist()
    {
        var documentId = Guid.NewGuid();
        _documentServiceMock.Setup(x => x.GetDocumentByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentResponseDto?)null);

        var result = await _controller.GetById(documentId);
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Create_ShouldReturnCreated_WhenValid()
    {
        var createDto = new CreateDocumentDto { Title = "Doc", FileName = "file.pdf" };
        var validationResult = new ValidationResult();
        _createValidatorMock.Setup(x => x.ValidateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        var responseDto = new DocumentResponseDto { Id = Guid.NewGuid(), Title = "Doc" };
        _documentServiceMock.Setup(x => x.CreateDocumentAsync(createDto, It.IsAny<CancellationToken>()))
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
        var createDto = new CreateDocumentDto { Title = "", FileName = "" };
        var validationFailures = new List<ValidationFailure>
        {
            new("Title", "Title is required"),
            new("FileName", "File name is required")
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
        errors!.Select(e => e.ErrorMessage).Should().Contain(new[] { "Title is required", "File name is required" });
    }

    [Fact]
    public async Task Create_ShouldThrowInvalidOperationException_WhenServiceThrows()
    {
        var createDto = new CreateDocumentDto { Title = "Doc", FileName = "file.pdf" };
        var validationResult = new ValidationResult();
        _createValidatorMock.Setup(x => x.ValidateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        _documentServiceMock.Setup(x => x.CreateDocumentAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Business error"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _controller.Create(createDto));
    }

    [Fact]
    public async Task Update_ShouldReturnNoContent_WhenValid()
    {
        var id = Guid.NewGuid();
        var updateDto = new UpdateDocumentDto { Title = "Updated" };
        var validationResult = new ValidationResult();
        _updateValidatorMock.Setup(x => x.ValidateAsync(updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        _documentServiceMock.Setup(x => x.UpdateDocumentAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.Update(id, updateDto);
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Update_ShouldThrowKeyNotFoundException_WhenServiceThrows()
    {
        var id = Guid.NewGuid();
        var updateDto = new UpdateDocumentDto { Title = "Updated" };
        var validationResult = new ValidationResult();
        _updateValidatorMock.Setup(x => x.ValidateAsync(updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        _documentServiceMock.Setup(x => x.UpdateDocumentAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException());

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.Update(id, updateDto));
    }

    [Fact]
    public async Task Delete_ShouldReturnNoContent_WhenSuccess()
    {
        var id = Guid.NewGuid();
        _documentServiceMock.Setup(x => x.DeleteDocumentAsync(id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.Delete(id);
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_ShouldThrowKeyNotFoundException_WhenServiceThrows()
    {
        var id = Guid.NewGuid();
        _documentServiceMock.Setup(x => x.DeleteDocumentAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException());

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.Delete(id));
    }

    [Fact]
    public async Task GetDocumentLogs_ShouldReturnOk_WhenDocumentExists()
    {
        var id = Guid.NewGuid();
        var pagedResult = new PagedResult<ArchiveLogListItemDto>();
        _documentServiceMock.Setup(x => x.GetDocumentLogsAsync(id, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await _controller.GetDocumentLogs(id);
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(pagedResult);
    }

    [Fact]
    public async Task GetDocumentLogs_ShouldThrowKeyNotFoundException_WhenDocumentDoesNotExist()
    {
        var id = Guid.NewGuid();
        _documentServiceMock.Setup(x => x.GetDocumentLogsAsync(id, 1, 20, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException());

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.GetDocumentLogs(id));
    }

    [Fact]
    public async Task CreateBulk_ShouldReturnOk_WhenValid()
    {
        var createDtos = new List<CreateDocumentDto> { new() { Title = "Doc1", FileName = "1.pdf" } };
        var bulkResult = new BulkOperationResult<Guid>();
        _documentServiceMock.Setup(x => x.CreateBulkAsync(createDtos, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        var result = await _controller.CreateBulk(createDtos);
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(bulkResult);
    }

    [Fact]
    public async Task CreateBulk_ShouldReturnBadRequest_WhenListEmpty()
    {
        var result = await _controller.CreateBulk(new List<CreateDocumentDto>());
        var badRequest = result.Result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.StatusCode.Should().Be(400);
        badRequest.Value.Should().Be("Bulk request cannot be empty.");
    }

    [Fact]
    public async Task CreateBulk_ShouldReturnBadRequest_WhenListExceedsMaxSize()
    {
        var createDtos = new List<CreateDocumentDto>();
        for (var i = 0; i < 101; i++)
            createDtos.Add(new CreateDocumentDto());

        var result = await _controller.CreateBulk(createDtos);
        var badRequest = result.Result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.StatusCode.Should().Be(400);
        badRequest.Value.Should().Be("Too many items in bulk request. Maximum allowed: 100.");
    }

    [Fact]
    public async Task UpdateBulk_ShouldReturnOk_WhenValid()
    {
        var updateDtos = new List<UpdateBulkDocumentDto> { new() { Id = Guid.NewGuid(), Title = "Updated" } };
        var bulkResult = new BulkOperationResult<Guid>();
        _documentServiceMock.Setup(x => x.UpdateBulkAsync(updateDtos, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        var result = await _controller.UpdateBulk(updateDtos);
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(bulkResult);
    }

    [Fact]
    public async Task DeleteBulk_ShouldReturnOk_WhenValid()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var bulkResult = new BulkOperationResult<Guid>();
        _documentServiceMock.Setup(x => x.DeleteBulkAsync(ids, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        var result = await _controller.DeleteBulk(ids);
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(bulkResult);
    }

    [Fact]
    public async Task GetDocumentsCountByCategory_ShouldReturnOk()
    {
        var dict = new Dictionary<string, int> { { "Cat", 5 } };
        _documentServiceMock.Setup(x => x.GetDocumentsCountByCategoryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(dict);

        var result = await _controller.GetDocumentsCountByCategory(default);
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().Be(dict);
    }

    [Fact]
    public async Task GetDocumentsStatistics_ShouldReturnOk()
    {
        var stats = new DocumentsStatisticsDto();
        _documentServiceMock.Setup(x => x.GetDocumentsStatisticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(stats);

        var result = await _controller.GetDocumentsStatistics(default);
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().Be(stats);
    }
}