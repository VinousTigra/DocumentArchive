using AutoMapper;
using DocumentArchive.API.Controllers;
using DocumentArchive.Core.DTOs.Category;
using DocumentArchive.Core.DTOs.Document;
using DocumentArchive.Core.DTOs.Shared;
using DocumentArchive.Core.Interfaces.Services;
using DocumentArchive.Core.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace DocumentArchive.Tests.ControllersTests;

public class CategoriesControllerTests
{
    private readonly Mock<ICategoryService> _categoryServiceMock;
    private readonly Mock<ILogger<CategoriesController>> _loggerMock;
    private readonly CategoriesController _controller;

    public CategoriesControllerTests()
    {
        _categoryServiceMock = new Mock<ICategoryService>();
        _loggerMock = new Mock<ILogger<CategoriesController>>();
        _controller = new CategoriesController(_categoryServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetById_ShouldReturnOk_WhenCategoryExists()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var responseDto = new CategoryResponseDto { Id = categoryId, Name = "Test" };
        _categoryServiceMock.Setup(x => x.GetCategoryByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        // Act
        var result = await _controller.GetById(categoryId);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(responseDto);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenCategoryDoesNotExist()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        _categoryServiceMock.Setup(x => x.GetCategoryByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CategoryResponseDto?)null);

        // Act
        var result = await _controller.GetById(categoryId);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Create_ShouldReturnCreatedAtAction_WhenNameIsUnique()
    {
        // Arrange
        var createDto = new CreateCategoryDto { Name = "NewCat" };
        var responseDto = new CategoryResponseDto { Id = Guid.NewGuid(), Name = "NewCat" };
        _categoryServiceMock.Setup(x => x.CreateCategoryAsync(createDto, It.IsAny<CancellationToken>()))
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
    public async Task GetCategoryDocuments_ShouldReturnPagedResult_WhenCategoryExists()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var pagedResult = new PagedResult<DocumentListItemDto>
        {
            Items = new List<DocumentListItemDto>(),
            PageNumber = 1,
            PageSize = 10,
            TotalCount = 0
        };
        _categoryServiceMock.Setup(x => x.GetCategoryDocumentsAsync(categoryId, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetDocumentsByCategory(categoryId, 1, 10);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(pagedResult);
    }

    [Fact]
    public async Task GetCategoryDocuments_ShouldReturnNotFound_WhenCategoryDoesNotExist()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        _categoryServiceMock.Setup(x => x.GetCategoryDocumentsAsync(categoryId, 1, 10, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException());

        // Act
        var result = await _controller.GetDocumentsByCategory(categoryId, 1, 10);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }
}