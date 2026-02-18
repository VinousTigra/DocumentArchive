using AutoMapper;
using DocumentArchive.Core.DTOs.Document;
using DocumentArchive.Core.Models;
using DocumentArchive.Services.Mapping;
using DocumentArchive.Services.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace DocumentArchive.Tests.ServicesTests;

public class DocumentServiceTests : TestBase
{
    private readonly DocumentService _service;

    public DocumentServiceTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        var mapper = config.CreateMapper();
        _service = new DocumentService(Context, mapper, NullLogger<DocumentService>.Instance);
    }

    [Fact]
    public async Task CreateDocumentAsync_ShouldAddDocumentAndLog()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Username = "test", Email = "test@test.com" };
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        var dto = new CreateDocumentDto
        {
            Title = "Test Doc",
            Description = "Desc",
            FileName = "test.pdf",
            UserId = user.Id,
            CategoryId = null
        };

        // Act
        var result = await _service.CreateDocumentAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Test Doc");
        result.Description.Should().Be("Desc");

        var document = await Context.Documents.FindAsync(result.Id);
        document.Should().NotBeNull();
        document.Title.Should().Be("Test Doc");

        var log = await Context.ArchiveLogs.FirstOrDefaultAsync(l => l.DocumentId == result.Id);
        log.Should().NotBeNull();
        log.ActionType.Should().Be(ActionType.Created);
        log.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task CreateDocumentAsync_ShouldThrow_WhenCategoryNotFound()
    {
        // Arrange
        var dto = new CreateDocumentDto
        {
            Title = "Doc",
            FileName = "doc.pdf",
            CategoryId = Guid.NewGuid()
        };

        // Act
        Func<Task> act = async () => await _service.CreateDocumentAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Category with id {dto.CategoryId} not found");
    }

    [Fact]
    public async Task GetDocumentByIdAsync_ShouldReturnDto_WhenExists()
    {
        // Arrange
        var category = new Category { Id = Guid.NewGuid(), Name = "TestCat" };
        var user = new User { Id = Guid.NewGuid(), Username = "user", Email = "u@t.com" };
        var document = new Document
        {
            Id = Guid.NewGuid(),
            Title = "Doc1",
            FileName = "f1.pdf",
            CategoryId = category.Id,
            UserId = user.Id,
            Category = category,
            User = user
        };
        Context.Categories.Add(category);
        Context.Users.Add(user);
        Context.Documents.Add(document);
        await Context.SaveChangesAsync();

        // Act
        var result = await _service.GetDocumentByIdAsync(document.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(document.Id);
        result.Title.Should().Be("Doc1");
        result.CategoryName.Should().Be("TestCat");
        result.UserName.Should().Be("user");
    }

    [Fact]
    public async Task GetDocumentByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        var result = await _service.GetDocumentByIdAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateDocumentAsync_ShouldUpdateFields()
    {
        // Arrange
        var category = new Category { Id = Guid.NewGuid(), Name = "OldCat" };
        var document = new Document
        {
            Id = Guid.NewGuid(),
            Title = "Old",
            Description = "OldDesc",
            FileName = "old.pdf",
            CategoryId = category.Id
        };

        Context.Categories.Add(category);
        Context.Documents.Add(document);
        await Context.SaveChangesAsync();

        var updateDto = new UpdateDocumentDto
        {
            Title = "New Title",
            Description = "New Desc",
            FileName = "new.pdf",
            CategoryId = null
        };

        // Act
        await _service.UpdateDocumentAsync(document.Id, updateDto);

        // Assert
        var updated = await Context.Documents.FindAsync(document.Id);
        updated.Should().NotBeNull();
        updated.Title.Should().Be("New Title");
        updated.Description.Should().Be("New Desc");
        updated.FileName.Should().Be("new.pdf");
        updated.CategoryId.Should().BeNull();
        updated.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateDocumentAsync_ShouldThrow_WhenCategoryNotFound()
    {
        // Arrange
        var document = new Document { Id = Guid.NewGuid(), Title = "Doc", FileName = "f" };
        Context.Documents.Add(document);
        await Context.SaveChangesAsync();

        var updateDto = new UpdateDocumentDto { CategoryId = Guid.NewGuid() };

        // Act
        var act = async () => await _service.UpdateDocumentAsync(document.Id, updateDto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Category with id {updateDto.CategoryId} not found");
    }

    [Fact]
    public async Task DeleteDocumentAsync_ShouldRemoveDocument()
    {
        // Arrange
        var doc = new Document { Id = Guid.NewGuid(), Title = "ToDelete", FileName = "del.pdf" };
        Context.Documents.Add(doc);
        await Context.SaveChangesAsync();

        // Act
        await _service.DeleteDocumentAsync(doc.Id);

        // Assert
        var deleted = await Context.Documents.FindAsync(doc.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteDocumentAsync_ShouldThrow_WhenNotFound()
    {
        var act = async () => await _service.DeleteDocumentAsync(Guid.NewGuid());
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetDocumentsAsync_ShouldReturnPagedFilteredSorted()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Username = "u1", Email = "u@t.com" };
        var cat1 = new Category { Id = Guid.NewGuid(), Name = "Cat1" };
        var cat2 = new Category { Id = Guid.NewGuid(), Name = "Cat2" };
        Context.Users.Add(user);
        Context.Categories.AddRange(cat1, cat2);
        await Context.SaveChangesAsync();

        var docs = new List<Document>
        {
            new()
            {
                Title = "Alpha", Description = "desc a", UserId = user.Id, CategoryId = cat1.Id,
                UploadDate = DateTime.UtcNow.AddDays(-5)
            },
            new()
            {
                Title = "Beta", Description = "desc b", UserId = user.Id, CategoryId = cat1.Id,
                UploadDate = DateTime.UtcNow.AddDays(-4)
            },
            new()
            {
                Title = "Gamma", Description = "desc c", UserId = null, CategoryId = cat2.Id,
                UploadDate = DateTime.UtcNow.AddDays(-3)
            },
            new()
            {
                Title = "Delta", Description = "desc d", UserId = null, CategoryId = cat2.Id,
                UploadDate = DateTime.UtcNow.AddDays(-2)
            },
            new()
            {
                Title = "Epsilon", Description = "desc e", UserId = user.Id, CategoryId = null,
                UploadDate = DateTime.UtcNow.AddDays(-1)
            }
        };
        Context.Documents.AddRange(docs);
        await Context.SaveChangesAsync();

        // Act & Assert: пагинация
        var page1 = await _service.GetDocumentsAsync(1, 2, null, null, null, null, null, "title");
        page1.Items.Should().HaveCount(2);
        page1.TotalCount.Should().Be(5);
        page1.Items.First().Title.Should().Be("Alpha"); // сортировка по title asc

        // Фильтр по поиску
        var searchResult = await _service.GetDocumentsAsync(1, 10, "Beta", null, null, null, null, null);
        searchResult.Items.Should().HaveCount(1);
        searchResult.Items.First().Title.Should().Be("Beta");

        // Фильтр по категориям
        var catResult = await _service.GetDocumentsAsync(1, 10, null, new[] { cat1.Id }, null, null, null, null);
        catResult.Items.Should().HaveCount(2);
        catResult.Items.Select(d => d.Title).Should().Contain(new[] { "Alpha", "Beta" });

        // Фильтр по пользователю
        var userResult = await _service.GetDocumentsAsync(1, 10, null, null, user.Id, null, null, null);
        userResult.Items.Should().HaveCount(3);

        // Фильтр по дате
        var fromDate = DateTime.UtcNow.AddDays(-4).AddSeconds(-10);
        var toDate = DateTime.UtcNow.AddDays(-2);
        var dateResult = await _service.GetDocumentsAsync(1, 10, null, null, null, fromDate, toDate, null);
        dateResult.Items.Should().HaveCount(3); // Beta, Gamma, Delta
    }

    [Fact]
    public async Task GetDocumentsCountByCategoryAsync_ShouldReturnCorrectCounts()
    {
        // Arrange
        var cat1 = new Category { Id = Guid.NewGuid(), Name = "Cat1" };
        var cat2 = new Category { Id = Guid.NewGuid(), Name = "Cat2" };
        Context.Categories.AddRange(cat1, cat2);
        await Context.SaveChangesAsync();

        Context.Documents.AddRange(
            new Document { Title = "D1", CategoryId = cat1.Id },
            new Document { Title = "D2", CategoryId = cat1.Id },
            new Document { Title = "D3", CategoryId = cat2.Id },
            new Document { Title = "D4" }
        );
        await Context.SaveChangesAsync();

        // Act
        var result = await _service.GetDocumentsCountByCategoryAsync();

        // Assert
        result.Should().ContainKey("Cat1").WhoseValue.Should().Be(2);
        result.Should().ContainKey("Cat2").WhoseValue.Should().Be(1);
        result.Should().NotContainKey(""); // категория null не включается
    }

    [Fact]
    public async Task GetDocumentsStatisticsAsync_ShouldReturnSummary()
    {
        // Arrange
        var cat = new Category { Id = Guid.NewGuid(), Name = "Cat" };
        Context.Categories.Add(cat);
        await Context.SaveChangesAsync();

        Context.Documents.AddRange(
            new Document { Title = "Doc1", CategoryId = cat.Id, UploadDate = DateTime.UtcNow.AddHours(-1) },
            new Document { Title = "Doc2", CategoryId = cat.Id, UploadDate = DateTime.UtcNow.AddHours(-2) },
            new Document { Title = "Doc3", CategoryId = null, UploadDate = DateTime.UtcNow.AddHours(-3) }
        );
        await Context.SaveChangesAsync();

        // Act
        var stats = await _service.GetDocumentsStatisticsAsync();

        // Assert
        stats.TotalDocuments.Should().Be(3);
        stats.DocumentsPerCategory.Should().HaveCount(1);
        stats.DocumentsPerCategory.First().CategoryName.Should().Be("Cat");
        stats.DocumentsPerCategory.First().Count.Should().Be(2);
        stats.LastUploadedDocument.Should().NotBeNull();
        stats.LastUploadedDocument!.Title.Should().Be("Doc1");
    }

    [Fact]
    public async Task CreateBulkAsync_ShouldCreateAllInTransaction()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Username = "bulk", Email = "b@t.com" };
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        var dtos = new List<CreateDocumentDto>
        {
            new() { Title = "B1", FileName = "1.pdf", UserId = user.Id },
            new() { Title = "B2", FileName = "2.pdf", UserId = user.Id }
        };

        // Act
        var result = await _service.CreateBulkAsync(dtos);

        // Assert
        result.Results.Should().HaveCount(2);
        result.Results.All(r => r.Success).Should().BeTrue();

        var docs = await Context.Documents.ToListAsync();
        docs.Should().HaveCount(2);
        docs.Select(d => d.Title).Should().Contain(new[] { "B1", "B2" });
    }

    [Fact]
    public async Task CreateBulkAsync_ShouldRollbackOnFailure()
    {
        // Arrange – один dto с несуществующим пользователем
        var dtos = new List<CreateDocumentDto>
        {
            new() { Title = "Good", FileName = "g.pdf", UserId = Guid.NewGuid() },
            new() { Title = "Bad", FileName = "b.pdf", UserId = Guid.NewGuid() } // оба не существуют
        };

        // Act
        var result = await _service.CreateBulkAsync(dtos);

        // Assert: оба должны быть с ошибкой, транзакция откатилась, документы не созданы
        result.Results.Should().HaveCount(2);
        result.Results.All(r => r.Success).Should().BeFalse();

        var docs = await Context.Documents.ToListAsync();
        docs.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateBulkAsync_ShouldUpdateMultiple()
    {
        // Arrange
        var doc1 = new Document { Id = Guid.NewGuid(), Title = "Old1", FileName = "o1.pdf" };
        var doc2 = new Document { Id = Guid.NewGuid(), Title = "Old2", FileName = "o2.pdf" };
        Context.Documents.AddRange(doc1, doc2);
        await Context.SaveChangesAsync();

        var updates = new List<UpdateBulkDocumentDto>
        {
            new() { Id = doc1.Id, Title = "New1", FileName = "n1.pdf" },
            new() { Id = doc2.Id, Title = "New2", FileName = "n2.pdf" }
        };

        // Act
        var result = await _service.UpdateBulkAsync(updates);

        // Assert
        result.Results.Should().HaveCount(2);
        result.Results.All(r => r.Success).Should().BeTrue();

        var updated1 = await Context.Documents.FindAsync(doc1.Id);
        updated1!.Title.Should().Be("New1");
        var updated2 = await Context.Documents.FindAsync(doc2.Id);
        updated2!.Title.Should().Be("New2");
    }

    [Fact]
    public async Task DeleteBulkAsync_ShouldDeleteMultiple()
    {
        // Arrange
        var doc1 = new Document { Id = Guid.NewGuid(), Title = "D1", FileName = "d1.pdf" };
        var doc2 = new Document { Id = Guid.NewGuid(), Title = "D2", FileName = "d2.pdf" };
        Context.Documents.AddRange(doc1, doc2);
        await Context.SaveChangesAsync();

        var ids = new[] { doc1.Id, doc2.Id };

        // Act
        var result = await _service.DeleteBulkAsync(ids);

        // Assert
        result.Results.Should().HaveCount(2);
        result.Results.All(r => r.Success).Should().BeTrue();

        var remaining = await Context.Documents.ToListAsync();
        remaining.Should().BeEmpty();
    }
}