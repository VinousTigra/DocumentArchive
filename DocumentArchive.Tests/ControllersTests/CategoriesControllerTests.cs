using AutoMapper;
using DocumentArchive.API.Controllers;
using DocumentArchive.Core.DTOs.Category;
using DocumentArchive.Core.Interfaces;
using DocumentArchive.Core.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DocumentArchive.Tests.ControllersTests;

public class CategoriesControllerTests
{
    private readonly Mock<ICategoryRepository> _categoryRepoMock;
    private readonly Mock<IDocumentRepository> _documentRepoMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly CategoriesController _controller;

    public CategoriesControllerTests()
    {
        _categoryRepoMock = new Mock<ICategoryRepository>();
        _documentRepoMock = new Mock<IDocumentRepository>();
        _mapperMock = new Mock<IMapper>();
        _controller = new CategoriesController(
            _categoryRepoMock.Object,
            _documentRepoMock.Object,
            _mapperMock.Object);
    }

    [Fact]
    public async Task GetById_ShouldReturnOk_WhenCategoryExists()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = new Category { Id = categoryId, Name = "Test" };
        var responseDto = new CategoryResponseDto { Id = categoryId, Name = "Test" };
        _categoryRepoMock.Setup(x => x.GetByIdAsync(categoryId)).ReturnsAsync(category);
        _mapperMock.Setup(x => x.Map<CategoryResponseDto>(category)).Returns(responseDto);

        // Act
        var result = await _controller.GetById(categoryId);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Create_ShouldReturnCreatedAtAction_WhenNameIsUnique()
    {
        // Arrange
        var createDto = new CreateCategoryDto { Name = "NewCat" };
        _categoryRepoMock.Setup(x => x.FindByNameAsync(createDto.Name))
            .ReturnsAsync((Category?)null);
        var category = new Category { Id = Guid.NewGuid(), Name = "NewCat" };
        var responseDto = new CategoryResponseDto { Id = category.Id, Name = "NewCat" };
        _mapperMock.Setup(x => x.Map<Category>(createDto)).Returns(category);
        _mapperMock.Setup(x => x.Map<CategoryResponseDto>(category)).Returns(responseDto);
        _categoryRepoMock.Setup(x => x.AddAsync(category)).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult.Should().NotBeNull();
        createdResult!.StatusCode.Should().Be(201);
    }
}