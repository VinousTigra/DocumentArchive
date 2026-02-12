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
        Environment.CurrentDirectory = _testDirectory;
        _repository = new CategoryRepository();
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
            Description = "Financial documents"
        };

        // Act
        await _repository.AddAsync(category);
        var retrieved = await _repository.GetByIdAsync(category.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("Finance");
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
    public async Task UpdateAsync_ShouldModifyCategory()
    {
        // Arrange
        var category = new Category { Id = Guid.NewGuid(), Name = "OldName" };
        await _repository.AddAsync(category);
        category.Name = "NewName";

        // Act
        await _repository.UpdateAsync(category);
        var updated = await _repository.GetByIdAsync(category.Id);

        // Assert
        updated!.Name.Should().Be("NewName");
    }
}