using DocumentArchive.Core.Models;
using DocumentArchive.Infrastructure.Repositories;
using FluentAssertions;

namespace DocumentArchive.Tests.RepositoriesTests;

public class CategoryRepositoryTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly CategoryRepository _repository;

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
        // Arrange
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Finance",
            Description = "Financial documents",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await _repository.AddAsync(category);
        var retrieved = await _repository.GetByIdAsync(category.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("Finance");
        retrieved.Description.Should().Be("Financial documents");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllCategories()
    {
        // Arrange
        var cat1 = new Category { Id = Guid.NewGuid(), Name = "HR" };
        var cat2 = new Category { Id = Guid.NewGuid(), Name = "IT" };
        await _repository.AddAsync(cat1);
        await _repository.AddAsync(cat2);

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Select(c => c.Name).Should().Contain(new[] { "HR", "IT" });
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnCategory_WhenExists()
    {
        // Arrange
        var category = new Category { Id = Guid.NewGuid(), Name = "Test" };
        await _repository.AddAsync(category);

        // Act
        var result = await _repository.GetByIdAsync(category.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindByNameAsync_ShouldReturnCategory_WhenExists()
    {
        // Arrange
        var category = new Category { Id = Guid.NewGuid(), Name = "HR" };
        await _repository.AddAsync(category);

        // Act
        var result = await _repository.FindByNameAsync("HR");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("HR");
    }

    [Fact]
    public async Task FindByNameAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = await _repository.FindByNameAsync("NonExistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyCategory()
    {
        // Arrange
        var category = new Category { Id = Guid.NewGuid(), Name = "OldName" };
        await _repository.AddAsync(category);

        category.Name = "NewName";
        category.Description = "New Description";

        // Act
        await _repository.UpdateAsync(category);
        var updated = await _repository.GetByIdAsync(category.Id);

        // Assert
        updated!.Name.Should().Be("NewName");
        updated.Description.Should().Be("New Description");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveCategory()
    {
        // Arrange
        var category = new Category { Id = Guid.NewGuid(), Name = "ToDelete" };
        await _repository.AddAsync(category);

        // Act
        await _repository.DeleteAsync(category.Id);
        var result = await _repository.GetByIdAsync(category.Id);

        // Assert
        result.Should().BeNull();
    }
}