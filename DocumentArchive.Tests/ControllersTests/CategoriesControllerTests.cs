using DocumentArchive.API.Controllers;
using DocumentArchive.Core.DTOs.Category;
using DocumentArchive.Core.DTOs.Document;
using DocumentArchive.Core.DTOs.Shared;
using DocumentArchive.Core.Interfaces.Services;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace DocumentArchive.Tests.ControllersTests;

public class CategoriesControllerTests
{
    private readonly Mock<ICategoryService> _categoryServiceMock;
    private readonly Mock<IValidator<CreateCategoryDto>> _createValidatorMock;
    private readonly Mock<IValidator<UpdateCategoryDto>> _updateValidatorMock;
    private readonly Mock<ILogger<CategoriesController>> _loggerMock;
    private readonly CategoriesController _controller;

    public CategoriesControllerTests()
    {
        _categoryServiceMock = new Mock<ICategoryService>();
        _createValidatorMock = new Mock<IValidator<CreateCategoryDto>>();
        _updateValidatorMock = new Mock<IValidator<UpdateCategoryDto>>();
        _loggerMock = new Mock<ILogger<CategoriesController>>();
        _controller = new CategoriesController(
            _categoryServiceMock.Object,
            _createValidatorMock.Object,
            _updateValidatorMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOk_WhenParametersValid()
    {
        var pagedResult = new PagedResult<CategoryListItemDto>();
        _categoryServiceMock.Setup(x => x.GetCategoriesAsync(
                1, 10, null, "name", "asc", It.IsAny<CancellationToken>()))
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
    public async Task GetAll_ShouldReturnBadRequest_WhenSortOrderInvalid()
    {
        var result = await _controller.GetAll(sortOrder: "invalid");
        var badRequest = result.Result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.StatusCode.Should().Be(400);
        badRequest.Value.Should().Be("Sort order must be 'asc' or 'desc'.");
    }

    [Fact]
    public async Task GetAll_ShouldReturnBadRequest_WhenSortByInvalid()
    {
        var result = await _controller.GetAll(sortBy: "invalidfield");
        var badRequest = result.Result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.StatusCode.Should().Be(400);
        badRequest.Value.Should().Be("Invalid sort field. Allowed values: name, createdat.");
    }

    [Fact]
    public async Task GetById_ShouldReturnOk_WhenCategoryExists()
    {
        var categoryId = Guid.NewGuid();
        var responseDto = new CategoryResponseDto { Id = categoryId, Name = "Test" };
        _categoryServiceMock.Setup(x => x.GetCategoryByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        var result = await _controller.GetById(categoryId);
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(responseDto);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenCategoryDoesNotExist()
    {
        var categoryId = Guid.NewGuid();
        _categoryServiceMock.Setup(x => x.GetCategoryByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CategoryResponseDto?)null);

        var result = await _controller.GetById(categoryId);
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Create_ShouldReturnCreated_WhenValid()
    {
        var createDto = new CreateCategoryDto { Name = "NewCat" };
        var validationResult = new ValidationResult();
        _createValidatorMock.Setup(x => x.ValidateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        var responseDto = new CategoryResponseDto { Id = Guid.NewGuid(), Name = "NewCat" };
        _categoryServiceMock.Setup(x => x.CreateCategoryAsync(createDto, It.IsAny<CancellationToken>()))
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
        var createDto = new CreateCategoryDto { Name = "" };
        var validationFailures = new List<ValidationFailure>
        {
            new("Name", "Name is required")
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
        errors!.Select(e => e.ErrorMessage).Should().Contain(new[] { "Name is required" });
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenInvalidOperationException()
    {
        var createDto = new CreateCategoryDto { Name = "ExistingCat" };
        var validationResult = new ValidationResult();
        _createValidatorMock.Setup(x => x.ValidateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        _categoryServiceMock.Setup(x => x.CreateCategoryAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Category with name already exists."));

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
        var updateDto = new UpdateCategoryDto { Name = "Updated" };
        var validationResult = new ValidationResult();
        _updateValidatorMock.Setup(x => x.ValidateAsync(updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        _categoryServiceMock.Setup(x => x.UpdateCategoryAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.Update(id, updateDto);
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Update_ShouldReturnNotFound_WhenKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        var updateDto = new UpdateCategoryDto { Name = "Updated" };
        var validationResult = new ValidationResult();
        _updateValidatorMock.Setup(x => x.ValidateAsync(updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        _categoryServiceMock.Setup(x => x.UpdateCategoryAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException());

        var result = await _controller.Update(id, updateDto);
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_ShouldReturnNoContent_WhenSuccess()
    {
        var id = Guid.NewGuid();
        _categoryServiceMock.Setup(x => x.DeleteCategoryAsync(id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.Delete(id);
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        _categoryServiceMock.Setup(x => x.DeleteCategoryAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException());

        var result = await _controller.Delete(id);
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_ShouldReturnBadRequest_WhenInvalidOperationException()
    {
        var id = Guid.NewGuid();
        _categoryServiceMock.Setup(x => x.DeleteCategoryAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cannot delete category with documents."));

        var result = await _controller.Delete(id);
        var badRequest = result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.StatusCode.Should().Be(400);
        badRequest.Value.Should().Be("Operation cannot be completed due to business rule violation.");
    }

    [Fact]
    public async Task GetDocumentsByCategory_ShouldReturnOk_WhenCategoryExists()
    {
        var categoryId = Guid.NewGuid();
        var pagedResult = new PagedResult<DocumentListItemDto>();
        _categoryServiceMock.Setup(x => x.GetCategoryDocumentsAsync(categoryId, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await _controller.GetDocumentsByCategory(categoryId);
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(pagedResult);
    }

    [Fact]
    public async Task GetDocumentsByCategory_ShouldReturnNotFound_WhenCategoryDoesNotExist()
    {
        var categoryId = Guid.NewGuid();
        _categoryServiceMock.Setup(x => x.GetCategoryDocumentsAsync(categoryId, 1, 10, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException());

        var result = await _controller.GetDocumentsByCategory(categoryId);
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetDocumentsByCategory_ShouldReturnBadRequest_WhenPageInvalid()
    {
        var result = await _controller.GetDocumentsByCategory(Guid.NewGuid(), page: 0);
        var badRequest = result.Result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.StatusCode.Should().Be(400);
        badRequest.Value.Should().Be("Page must be >= 1.");
    }
}