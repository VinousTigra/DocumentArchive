using DocumentArchive.Core.Models;
using DocumentArchive.Infrastructure.Repositories;
using FluentAssertions;

namespace DocumentArchive.Tests.RepositoriesTests;

public class DocumentRepositoryTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly DocumentRepository _repository;

    public DocumentRepositoryTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
        _repository = new DocumentRepository(_testDirectory); // передаём временную папку
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
            Directory.Delete(_testDirectory, true);
    }

    [Fact]
    public async Task AddAsync_ShouldAddDocument()
    {
        // Arrange
        var doc = new Document
        {
            Id = Guid.NewGuid(),
            Title = "Test Document",
            FileName = "test.pdf",
            UploadDate = DateTime.UtcNow
        };

        // Act
        await _repository.AddAsync(doc);
        var retrieved = await _repository.GetByIdAsync(doc.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Title.Should().Be("Test Document");
        retrieved.FileName.Should().Be("test.pdf");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllDocuments()
    {
        // Arrange
        var doc1 = new Document { Id = Guid.NewGuid(), Title = "Doc1", FileName = "1.pdf" };
        var doc2 = new Document { Id = Guid.NewGuid(), Title = "Doc2", FileName = "2.pdf" };
        await _repository.AddAsync(doc1);
        await _repository.AddAsync(doc2);

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Select(d => d.Title).Should().Contain(new[] { "Doc1", "Doc2" });
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyDocument()
    {
        // Arrange
        var doc = new Document { Id = Guid.NewGuid(), Title = "Old", FileName = "old.pdf" };
        await _repository.AddAsync(doc);
        doc.Title = "New";
        doc.FileName = "new.pdf";

        // Act
        await _repository.UpdateAsync(doc);
        var updated = await _repository.GetByIdAsync(doc.Id);

        // Assert
        updated!.Title.Should().Be("New");
        updated.FileName.Should().Be("new.pdf");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveDocument()
    {
        // Arrange
        var doc = new Document { Id = Guid.NewGuid(), Title = "ToDelete", FileName = "del.pdf" };
        await _repository.AddAsync(doc);

        // Act
        await _repository.DeleteAsync(doc.Id);
        var result = await _repository.GetByIdAsync(doc.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenDocumentNotExists()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SearchAsync_ShouldFindByTitle()
    {
        // Arrange
        var doc1 = new Document { Id = Guid.NewGuid(), Title = "Alpha Report", FileName = "a.pdf" };
        var doc2 = new Document { Id = Guid.NewGuid(), Title = "Beta Summary", FileName = "b.pdf" };
        await _repository.AddAsync(doc1);
        await _repository.AddAsync(doc2);

        // Act
        var result = await _repository.SearchAsync("alpha");

        // Assert
        result.Should().ContainSingle();
        result.First().Title.Should().Be("Alpha Report");
    }

    [Fact]
    public async Task SearchAsync_ShouldFindByDescription()
    {
        // Arrange
        var doc1 = new Document { Id = Guid.NewGuid(), Title = "Doc1", Description = "Important report", FileName = "a.pdf" };
        var doc2 = new Document { Id = Guid.NewGuid(), Title = "Doc2", FileName = "b.pdf" };
        await _repository.AddAsync(doc1);
        await _repository.AddAsync(doc2);

        // Act
        var result = await _repository.SearchAsync("important");

        // Assert
        result.Should().ContainSingle();
        result.First().Id.Should().Be(doc1.Id);
    }

    [Fact]
    public async Task GetByCategoryAsync_ShouldFilterByCategory()
    {
        // Arrange
        var catId = Guid.NewGuid();
        var doc1 = new Document { Id = Guid.NewGuid(), Title = "Doc1", CategoryId = catId, FileName = "1.pdf" };
        var doc2 = new Document { Id = Guid.NewGuid(), Title = "Doc2", CategoryId = null, FileName = "2.pdf" };
        await _repository.AddAsync(doc1);
        await _repository.AddAsync(doc2);

        // Act
        var result = await _repository.GetByCategoryAsync(catId);

        // Assert
        result.Should().ContainSingle();
        result.First().Id.Should().Be(doc1.Id);
    }

    [Fact]
    public async Task GetByUserAsync_ShouldFilterByUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var doc1 = new Document { Id = Guid.NewGuid(), Title = "Doc1", UserId = userId, FileName = "1.pdf" };
        var doc2 = new Document { Id = Guid.NewGuid(), Title = "Doc2", UserId = null, FileName = "2.pdf" };
        await _repository.AddAsync(doc1);
        await _repository.AddAsync(doc2);

        // Act
        var result = await _repository.GetByUserAsync(userId);

        // Assert
        result.Should().ContainSingle();
        result.First().Id.Should().Be(doc1.Id);
    }
}