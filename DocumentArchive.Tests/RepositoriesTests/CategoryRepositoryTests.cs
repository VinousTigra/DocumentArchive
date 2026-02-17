using DocumentArchive.Core.Models;
using DocumentArchive.Infrastructure.Repositories;
using FluentAssertions;

namespace DocumentArchive.Tests.RepositoriesTests;

public class CategoryRepositoryTests : IDisposable
{
    private readonly CategoryRepository _repository;
    private readonly string _testDirectory;

    public CategoryRepositoryTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
        _repository = new CategoryRepository(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
            Directory.Delete(_testDirectory, true);
    }

    [Fact]
    public async Task AddAsync_ShouldAddCategory()
    {
        var category = new Category { Id = Guid.NewGuid(), Name = "Finance", Description = "Financial docs" };
        await _repository.AddAsync(category);
        var retrieved = await _repository.GetByIdAsync(category.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("Finance");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllCategories()
    {
        var cat1 = new Category { Id = Guid.NewGuid(), Name = "HR" };
        var cat2 = new Category { Id = Guid.NewGuid(), Name = "IT" };
        await _repository.AddAsync(cat1);
        await _repository.AddAsync(cat2);
        var result = await _repository.GetAllAsync();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyCategory()
    {
        var cat = new Category { Id = Guid.NewGuid(), Name = "Old" };
        await _repository.AddAsync(cat);
        cat.Name = "New";
        await _repository.UpdateAsync(cat);
        var updated = await _repository.GetByIdAsync(cat.Id);
        updated!.Name.Should().Be("New");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveCategory()
    {
        var cat = new Category { Id = Guid.NewGuid(), Name = "Temp" };
        await _repository.AddAsync(cat);
        await _repository.DeleteAsync(cat.Id);
        var result = await _repository.GetByIdAsync(cat.Id);
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindByNameAsync_ShouldReturnCategory_WhenExists()
    {
        var cat = new Category { Id = Guid.NewGuid(), Name = "HR" };
        await _repository.AddAsync(cat);
        var found = await _repository.FindByNameAsync("HR");
        found.Should().NotBeNull();
        found!.Name.Should().Be("HR");
    }

    [Fact]
    public async Task FindByNameAsync_ShouldReturnNull_WhenNotExists()
    {
        var found = await _repository.FindByNameAsync("None");
        found.Should().BeNull();
    }

    [Fact]
    public async Task GetPagedAsync_ShouldReturnPagedResult_WithFilteringAndSorting()
    {
        // Arrange
        var cats = new List<Category>();
        for (var i = 0; i < 10; i++)
            cats.Add(new Category
            {
                Id = Guid.NewGuid(),
                Name = $"Category{i}",
                Description = $"Desc{i}",
                CreatedAt = DateTime.UtcNow.AddDays(-i)
            });
        foreach (var c in cats)
            await _repository.AddAsync(c);

        // Act
        var result = await _repository.GetPagedAsync(
            2,
            3,
            "Category", // все подходят
            "name",
            "asc");

        // Assert
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(3);
        result.TotalCount.Should().Be(10);
        result.Items.Should().HaveCount(3);
        // Проверим сортировку по имени: Category0, Category1, ... Category9
        var expectedNames = cats.OrderBy(c => c.Name).Skip(3).Take(3).Select(c => c.Name).ToList();
        result.Items.Select(c => c.Name).Should().Equal(expectedNames);
    }
}