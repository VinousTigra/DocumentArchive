using AutoMapper;
using DocumentArchive.Core.DTOs.Category;
using DocumentArchive.Core.Models;
using DocumentArchive.Services.Mapping;
using DocumentArchive.Services.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace DocumentArchive.Tests.ServicesTests;

public class CategoryServiceTests : TestBase
{
    private readonly IMapper _mapper;
    private readonly CategoryService _service;

    public CategoryServiceTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();
        _service = new CategoryService(Context, _mapper, NullLogger<CategoryService>.Instance);
    }

    [Fact]
    public async Task CreateCategoryAsync_ShouldAddCategory()
    {
        // Arrange
        var dto = new CreateCategoryDto
        {
            Name = "New Category",
            Description = "Some description"
        };

        // Act
        var result = await _service.CreateCategoryAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New Category");
        result.Description.Should().Be("Some description");

        var category = await Context.Categories.FirstOrDefaultAsync(c => c.Name == "New Category");
        category.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateCategoryAsync_ShouldThrow_WhenNameExists()
    {
        // Arrange
        var existing = new Category { Id = Guid.NewGuid(), Name = "Existing" };
        Context.Categories.Add(existing);
        await Context.SaveChangesAsync();

        var dto = new CreateCategoryDto { Name = "Existing" };

        // Act
        Func<Task> act = async () => await _service.CreateCategoryAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Category with name 'Existing' already exists.");
    }

    [Fact]
    public async Task GetCategoryByIdAsync_ShouldReturnDto()
    {
        // Arrange
        var cat = new Category { Id = Guid.NewGuid(), Name = "Test", Description = "Desc" };
        Context.Categories.Add(cat);
        await Context.SaveChangesAsync();

        // Act
        var result = await _service.GetCategoryByIdAsync(cat.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(cat.Id);
        result.Name.Should().Be("Test");
        result.Description.Should().Be("Desc");
    }

    [Fact]
    public async Task UpdateCategoryAsync_ShouldUpdate()
    {
        // Arrange
        var cat = new Category { Id = Guid.NewGuid(), Name = "Old", Description = "OldDesc" };
        Context.Categories.Add(cat);
        await Context.SaveChangesAsync();

        var updateDto = new UpdateCategoryDto
        {
            Name = "New",
            Description = "NewDesc"
        };

        // Act
        await _service.UpdateCategoryAsync(cat.Id, updateDto);

        // Assert
        var updated = await Context.Categories.FindAsync(cat.Id);
        updated!.Name.Should().Be("New");
        updated.Description.Should().Be("NewDesc");
        updated.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateCategoryAsync_ShouldThrow_WhenNameExists()
    {
        // Arrange
        var cat1 = new Category { Id = Guid.NewGuid(), Name = "Cat1" };
        var cat2 = new Category { Id = Guid.NewGuid(), Name = "Cat2" };
        Context.Categories.AddRange(cat1, cat2);
        await Context.SaveChangesAsync();

        var updateDto = new UpdateCategoryDto { Name = "Cat2" };

        // Act
        var act = async () => await _service.UpdateCategoryAsync(cat1.Id, updateDto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Category with name 'Cat2' already exists.");
    }

    [Fact]
    public async Task DeleteCategoryAsync_ShouldRemove_WhenNoDocuments()
    {
        // Arrange
        var cat = new Category { Id = Guid.NewGuid(), Name = "ToDelete" };
        Context.Categories.Add(cat);
        await Context.SaveChangesAsync();

        // Act
        await _service.DeleteCategoryAsync(cat.Id);

        // Assert
        var deleted = await Context.Categories.FindAsync(cat.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteCategoryAsync_ShouldThrow_WhenHasDocuments()
    {
        // Arrange
        var cat = new Category { Id = Guid.NewGuid(), Name = "WithDocs" };
        Context.Categories.Add(cat);
        Context.Documents.Add(new Document { Title = "Doc", CategoryId = cat.Id });
        await Context.SaveChangesAsync();

        // Act
        var act = async () => await _service.DeleteCategoryAsync(cat.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot delete category with existing documents.");
    }

    /*[Fact]
    public async Task GetCategoriesAsync_ShouldReturnPagedFilteredSorted()
    {
        // Arrange
        Context.Categories.AddRange(
            new Category { Name = "Alpha", Description = "A", CreatedAt = DateTime.UtcNow.AddDays(-3) },
            new Category { Name = "Beta", Description = "B", CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new Category { Name = "Gamma", Description = "G", CreatedAt = DateTime.UtcNow.AddDays(-1) }
        );
        await Context.SaveChangesAsync();

        // Act: пагинация
        var page1 = await _service.GetCategoriesAsync(1, 2, null, "name", "asc");
        page1.Items.Should().HaveCount(2);
        page1.TotalCount.Should().Be(3);
        page1.Items.First().Name.Should().Be("Alpha");

        // Фильтр поиск
        var search = await _service.GetCategoriesAsync(1, 10, "bet", "name", "asc");
        search.Items.Should().HaveCount(1);
        search.Items.First().Name.Should().Be("Beta");

        // Сортировка по дате убывания
        var sorted = await _service.GetCategoriesAsync(1, 10, null, "createdat", "desc");
        sorted.Items.First().Name.Should().Be("Gamma");
    }*/

    [Fact]
    public async Task GetCategoriesWithDocumentCountAsync_ShouldReturnCounts()
    {
        // Arrange
        var cat1 = new Category { Id = Guid.NewGuid(), Name = "Cat1" };
        var cat2 = new Category { Id = Guid.NewGuid(), Name = "Cat2" };
        Context.Categories.AddRange(cat1, cat2);
        await Context.SaveChangesAsync();

        Context.Documents.AddRange(
            new Document { Title = "D1", CategoryId = cat1.Id },
            new Document { Title = "D2", CategoryId = cat1.Id },
            new Document { Title = "D3", CategoryId = cat2.Id }
        );
        await Context.SaveChangesAsync();

        // Act
        var result = await _service.GetCategoriesWithDocumentCountAsync();

        // Assert
        result.Should().HaveCount(2);
        result.First(c => c.Name == "Cat1").DocumentsCount.Should().Be(2);
        result.First(c => c.Name == "Cat2").DocumentsCount.Should().Be(1);
    }

    [Fact]
    public async Task GetCategoryDocumentsAsync_ShouldReturnPaged()
    {
        // Arrange
        var cat = new Category { Id = Guid.NewGuid(), Name = "Cat" };
        Context.Categories.Add(cat);
        for (var i = 0; i < 10; i++) Context.Documents.Add(new Document { Title = $"Doc{i}", CategoryId = cat.Id });
        await Context.SaveChangesAsync();

        // Act
        var result = await _service.GetCategoryDocumentsAsync(cat.Id, 2, 3);

        // Assert
        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(10);
        result.PageNumber.Should().Be(2);
    }
}