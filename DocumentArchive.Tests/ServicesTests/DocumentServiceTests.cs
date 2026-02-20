using DocumentArchive.Core.DTOs.Document;
using DocumentArchive.Core.Models;
using DocumentArchive.Services.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace DocumentArchive.Tests.ServicesTests;

public class DocumentServiceTests : TestBase
{
    private readonly DocumentService _service;

    public DocumentServiceTests()
    {
        var mapper = TestHelper.CreateMapper();
        _service = new DocumentService(Context, mapper, NullLogger<DocumentService>.Instance);
    }

    protected override void SeedData()
    {
        var user1 = new User { Id = Guid.NewGuid(), Username = "user1", Email = "u1@test.com", IsActive = true };
        var user2 = new User { Id = Guid.NewGuid(), Username = "user2", Email = "u2@test.com", IsActive = true };
        var cat = new Category { Id = Guid.NewGuid(), Name = "TestCat" };
        Context.Users.AddRange(user1, user2);
        Context.Categories.Add(cat);
        Context.Documents.AddRange(
            new Document
            {
                Id = Guid.NewGuid(), Title = "Doc1", UserId = user1.Id, CategoryId = cat.Id,
                UploadDate = DateTime.UtcNow.AddDays(-5)
            },
            new Document
                { Id = Guid.NewGuid(), Title = "Doc2", UserId = user1.Id, UploadDate = DateTime.UtcNow.AddDays(-2) },
            new Document
                { Id = Guid.NewGuid(), Title = "Doc3", UserId = user2.Id, UploadDate = DateTime.UtcNow.AddDays(-1) }
        );
        Context.SaveChanges();
    }

    [Fact]
    public async Task GetDocumentsAsync_UserCanViewOwn_ShouldReturnOwn()
    {
        var user = Context.Users.First(u => u.Username == "user1");
        var permissions = new List<string>(); // нет права ViewAnyDocument
        var result = await _service.GetDocumentsAsync(1, 10, null, null, null, null, null, null, user.Id, permissions,
            CancellationToken.None);
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetDocumentsAsync_AdminWithViewAny_ShouldReturnAll()
    {
        var adminId = Guid.NewGuid(); // не владелец
        var permissions = new List<string> { "ViewAnyDocument" };
        var result = await _service.GetDocumentsAsync(1, 10, null, null, null, null, null, null, adminId, permissions,
            CancellationToken.None);
        result.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetDocumentByIdAsync_OwnDocument_ShouldReturn()
    {
        var user = Context.Users.First();
        var doc = Context.Documents.First(d => d.UserId == user.Id);
        var permissions = new List<string>();
        var result = await _service.GetDocumentByIdAsync(doc.Id, user.Id, permissions, CancellationToken.None);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetDocumentByIdAsync_OtherUserDocument_ShouldReturnNull()
    {
        var user = Context.Users.First(u => u.Username == "user1");
        var otherDoc = Context.Documents.First(d => d.UserId != user.Id);
        var permissions = new List<string>();
        var result = await _service.GetDocumentByIdAsync(otherDoc.Id, user.Id, permissions, CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateDocumentAsync_ShouldSetCurrentUser()
    {
        var user = Context.Users.First();
        var dto = new CreateDocumentDto
            { Title = "New Doc", FileName = "new.pdf", CategoryId = Context.Categories.First().Id };
        var result = await _service.CreateDocumentAsync(dto, user.Id, CancellationToken.None);
        result.Title.Should().Be("New Doc");
        var doc = await Context.Documents.FindAsync(result.Id);
        doc!.UserId.Should().Be(user.Id);
        Context.ArchiveLogs.Count(l => l.DocumentId == result.Id).Should().Be(1);
    }

    [Fact]
    public async Task UpdateDocumentAsync_OwnDocument_ShouldUpdate()
    {
        var user = Context.Users.First();
        var doc = Context.Documents.First(d => d.UserId == user.Id);
        var permissions = new List<string> { "EditOwnDocuments" };
        var dto = new UpdateDocumentDto 
        { 
            Title = "Updated", 
            Description = "New desc",
            FileName = doc.FileName // сохраняем существующее имя
        };
        await _service.UpdateDocumentAsync(doc.Id, dto, user.Id, permissions, default);
        var updated = await Context.Documents.FindAsync(doc.Id);
        updated!.Title.Should().Be("Updated");
        updated.Description.Should().Be("New desc");
    }



    [Fact]
    public async Task UpdateDocumentAsync_OtherUserDocument_WithoutPermission_ShouldThrow()
    {
        var user = Context.Users.First(u => u.Username == "user1");
        var otherDoc = Context.Documents.First(d => d.UserId != user.Id);
        var permissions = new List<string>();
        var dto = new UpdateDocumentDto { Title = "Hack" };
        await FluentActions.Invoking(() =>
                _service.UpdateDocumentAsync(otherDoc.Id, dto, user.Id, permissions, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task DeleteDocumentAsync_OwnDocument_ShouldDelete()
    {
        var user = Context.Users.First();
        var doc = new Document { Id = Guid.NewGuid(), Title = "Temp", UserId = user.Id, UploadDate = DateTime.UtcNow };
        Context.Documents.Add(doc);
        await Context.SaveChangesAsync();
    
        var permissions = new List<string> { "DeleteOwnDocuments" };
        await _service.DeleteDocumentAsync(doc.Id, user.Id, permissions, default);
    
        Context.Documents.Any(d => d.Id == doc.Id).Should().BeFalse();
        // После удаления документа логи с этим DocumentId должны отсутствовать (каскадное удаление)
        Context.ArchiveLogs.Count(l => l.DocumentId == doc.Id).Should().Be(0);
    }

    [Fact]
    public async Task DeleteDocumentAsync_OldDocument_ShouldThrowBusinessRule()
    {
        var user = Context.Users.First();
        var doc = new Document
            { Id = Guid.NewGuid(), Title = "Old", UserId = user.Id, UploadDate = DateTime.UtcNow.AddDays(-31) };
        Context.Documents.Add(doc);
        await Context.SaveChangesAsync();
        var permissions = new List<string> { "DeleteOwnDocuments" };
        await FluentActions.Invoking(() =>
                _service.DeleteDocumentAsync(doc.Id, user.Id, permissions, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*30 days ago*");
    }

    [Fact]
    public async Task GetDocumentLogsAsync_OwnDocument_ShouldReturn()
    {
        var user = Context.Users.First();
        var doc = Context.Documents.First(d => d.UserId == user.Id);
        var permissions = new List<string>();
        var result = await _service.GetDocumentLogsAsync(doc.Id, 1, 10, user.Id, permissions, CancellationToken.None);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateBulkAsync_MixedSuccess_ShouldReturnResults()
    {
        var user = Context.Users.First();
        var permissions = new List<string>();
        var cat = Context.Categories.First();
        var validDto = new CreateDocumentDto { Title = "Valid", FileName = "v.pdf", CategoryId = cat.Id };
        var invalidDto = new CreateDocumentDto
            { Title = "Invalid", FileName = "i.pdf", CategoryId = Guid.NewGuid() }; // несуществующая категория
        var dtos = new List<CreateDocumentDto> { validDto, invalidDto };
        var result = await _service.CreateBulkAsync(dtos, user.Id, permissions, CancellationToken.None);
        result.Results.Should().HaveCount(2);
        result.Results[0].Success.Should().BeTrue();
        result.Results[1].Success.Should().BeFalse();
        result.Results[1].Error.Should().Contain("not found");
        Context.Documents.Count().Should().Be(4); // изначально 3 + 1 успешный
    }

    [Fact]
    public async Task UpdateBulkAsync_ShouldUpdateMultiple()
    {
        var user = Context.Users.First();
        var docs = Context.Documents.Where(d => d.UserId == user.Id).Take(2).ToList();
        var permissions = new List<string> { "EditOwnDocuments" };
        var cat = Context.Categories.First();
        var updateDtos = docs.Select(d => new UpdateBulkDocumentDto
        {
            Id = d.Id,
            Title = d.Title + "_updated",
            CategoryId = cat.Id
        }).ToList();
        var result = await _service.UpdateBulkAsync(updateDtos, user.Id, permissions, CancellationToken.None);
        result.Results.All(r => r.Success).Should().BeTrue();
        var updated1 = await Context.Documents.FindAsync(docs[0].Id);
        updated1!.Title.Should().EndWith("_updated");
    }

    [Fact]
    public async Task DeleteBulkAsync_ShouldDeleteMultiple()
    {
        var user = Context.Users.First();
        // создадим документы для удаления
        var docs = new List<Document>();
        for (var i = 0; i < 3; i++)
        {
            var doc = new Document
                { Id = Guid.NewGuid(), Title = $"Del{i}", UserId = user.Id, UploadDate = DateTime.UtcNow };
            Context.Documents.Add(doc);
            docs.Add(doc);
        }

        await Context.SaveChangesAsync();
        var permissions = new List<string> { "DeleteOwnDocuments" };
        var ids = docs.Select(d => d.Id).ToList();
        var result = await _service.DeleteBulkAsync(ids, user.Id, permissions, CancellationToken.None);
        result.Results.All(r => r.Success).Should().BeTrue();
        Context.Documents.Count(d => ids.Contains(d.Id)).Should().Be(0);
    }

    [Fact]
    public async Task GetDocumentsCountByCategoryAsync_ShouldGroup()
    {
        var result = await _service.GetDocumentsCountByCategoryAsync(CancellationToken.None);
        result.Should().ContainKey("TestCat");
    }

    [Fact]
    public async Task GetDocumentsStatisticsAsync_ShouldReturnStats()
    {
        var stats = await _service.GetDocumentsStatisticsAsync(CancellationToken.None);
        stats.TotalDocuments.Should().Be(3);
        stats.DocumentsPerCategory.Should().HaveCount(1);
        stats.LastUploadedDocument.Should().NotBeNull();
    }
}