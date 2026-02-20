using DocumentArchive.Core.DTOs.Category;
using DocumentArchive.Core.Models;
using DocumentArchive.Services.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace DocumentArchive.Tests.ServicesTests;

public class CategoryServiceTests : TestBase
{
    private readonly CategoryService _service;

    public CategoryServiceTests()
    {
        var mapper = TestHelper.CreateMapper();
        _service = new CategoryService(Context, mapper, NullLogger<CategoryService>.Instance);
    }

    protected override void SeedData()
    {
        Context.Categories.AddRange(
            new Category
            {
                Id = Guid.NewGuid(), Name = "Work", Description = "Work documents",
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            },
            new Category
            {
                Id = Guid.NewGuid(), Name = "Personal", Description = "Personal documents",
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            }
        );
        Context.SaveChanges();
    }

    [Fact]
    public async Task GetCategoriesAsync_WithoutParams_ShouldReturnAll()
    {
        var result = await _service.GetCategoriesAsync(1, 10, null, null, null, CancellationToken.None);
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetCategoriesAsync_WithSearch_ShouldFilter()
    {
        var result = await _service.GetCategoriesAsync(1, 10, "Work", null, null, CancellationToken.None);
        result.Items.Should().HaveCount(1);
        result.Items.First().Name.Should().Be("Work");
    }

    [Fact]
    public async Task GetCategoriesAsync_WithSort_ShouldOrder()
    {
        var result = await _service.GetCategoriesAsync(1, 10, null, "name", "desc", CancellationToken.None);
        result.Items.First().Name.Should().Be("Work"); // Work > Personal по алфавиту?
        // Проверим, что порядок корректный
        var names = result.Items.Select(c => c.Name).ToList();
        names.Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task GetCategoryByIdAsync_Existing_ShouldReturn()
    {
        var id = Context.Categories.First().Id;
        var result = await _service.GetCategoryByIdAsync(id, CancellationToken.None);
        result.Should().NotBeNull();
        result.Id.Should().Be(id);
    }

    [Fact]
    public async Task GetCategoryByIdAsync_NonExisting_ShouldReturnNull()
    {
        var result = await _service.GetCategoryByIdAsync(Guid.NewGuid(), CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateCategoryAsync_ValidDto_ShouldCreate()
    {
        var dto = new CreateCategoryDto { Name = "New", Description = "New category" };
        var result = await _service.CreateCategoryAsync(dto, CancellationToken.None);
        result.Name.Should().Be("New");
        Context.Categories.Count().Should().Be(3);
    }

    [Fact]
    public async Task CreateCategoryAsync_DuplicateName_ShouldThrow()
    {
        var dto = new CreateCategoryDto { Name = "Work" };
        await FluentActions.Invoking(() => _service.CreateCategoryAsync(dto, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task UpdateCategoryAsync_ValidDto_ShouldUpdate()
    {
        var id = Context.Categories.First(c => c.Name == "Work").Id;
        var dto = new UpdateCategoryDto { Name = "UpdatedWork", Description = "Updated desc" };
        await _service.UpdateCategoryAsync(id, dto, CancellationToken.None);
        var updated = await Context.Categories.FindAsync(id);
        updated!.Name.Should().Be("UpdatedWork");
        updated.Description.Should().Be("Updated desc");
    }

    [Fact]
    public async Task UpdateCategoryAsync_NonExisting_ShouldThrow()
    {
        var dto = new UpdateCategoryDto { Name = "Test" };
        await FluentActions.Invoking(() => _service.UpdateCategoryAsync(Guid.NewGuid(), dto, CancellationToken.None))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteCategoryAsync_WithoutDocuments_ShouldDelete()
    {
        var id = Context.Categories.First().Id;
        await _service.DeleteCategoryAsync(id, CancellationToken.None);
        Context.Categories.Count().Should().Be(1);
    }

    [Fact]
    public async Task DeleteCategoryAsync_WithDocuments_ShouldThrow()
    {
        var cat = Context.Categories.First();
        Context.Documents.Add(new Document { Id = Guid.NewGuid(), Title = "Doc", CategoryId = cat.Id });
        await Context.SaveChangesAsync();
        await FluentActions.Invoking(() => _service.DeleteCategoryAsync(cat.Id, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*existing documents*");
    }

    [Fact]
    public async Task GetCategoryDocumentsAsync_ShouldReturnPaged()
    {
        var cat = Context.Categories.First();
        for (var i = 0; i < 5; i++)
            Context.Documents.Add(new Document { Id = Guid.NewGuid(), Title = $"Doc{i}", CategoryId = cat.Id });
        await Context.SaveChangesAsync();
        var result = await _service.GetCategoryDocumentsAsync(cat.Id, 1, 3, CancellationToken.None);
        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(5);
    }

    [Fact]
    public async Task GetCategoriesWithDocumentCountAsync_ShouldReturnCounts()
    {
        var cat1 = Context.Categories.First();
        var cat2 = Context.Categories.Last();
        Context.Documents.Add(new Document { Id = Guid.NewGuid(), Title = "Doc1", CategoryId = cat1.Id });
        Context.Documents.Add(new Document { Id = Guid.NewGuid(), Title = "Doc2", CategoryId = cat1.Id });
        Context.Documents.Add(new Document { Id = Guid.NewGuid(), Title = "Doc3", CategoryId = cat2.Id });
        await Context.SaveChangesAsync();
        var result = await _service.GetCategoriesWithDocumentCountAsync(CancellationToken.None);
        result.First(c => c.Id == cat1.Id).DocumentsCount.Should().Be(2);
        result.First(c => c.Id == cat2.Id).DocumentsCount.Should().Be(1);
    }
}