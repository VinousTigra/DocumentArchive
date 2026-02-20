using DocumentArchive.Core.DTOs.DocumentVersion;
using DocumentArchive.Core.Models;
using DocumentArchive.Services.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace DocumentArchive.Tests.ServicesTests;

public class DocumentVersionServiceTests : TestBase
{
    private readonly DocumentVersionService _service;

    public DocumentVersionServiceTests()
    {
        var mapper = TestHelper.CreateMapper();
        _service = new DocumentVersionService(Context, mapper, NullLogger<DocumentVersionService>.Instance);
    }

    protected override void SeedData()
    {
        var user = new User { Id = Guid.NewGuid(), Username = "user", Email = "u@test.com" };
        var doc1 = new Document { Id = Guid.NewGuid(), Title = "Doc1", UserId = user.Id };
        var doc2 = new Document { Id = Guid.NewGuid(), Title = "Doc2", UserId = user.Id };
        Context.Users.Add(user);
        Context.Documents.AddRange(doc1, doc2);
        Context.DocumentVersions.AddRange(
            new DocumentVersion
            {
                Id = Guid.NewGuid(), DocumentId = doc1.Id, VersionNumber = 1, FileName = "v1.pdf", UploadedBy = user.Id
            },
            new DocumentVersion
            {
                Id = Guid.NewGuid(), DocumentId = doc1.Id, VersionNumber = 2, FileName = "v2.pdf", UploadedBy = user.Id
            },
            new DocumentVersion
            {
                Id = Guid.NewGuid(), DocumentId = doc2.Id, VersionNumber = 1, FileName = "v1.pdf", UploadedBy = user.Id
            }
        );
        Context.SaveChanges();
    }

    [Fact]
    public async Task GetAllAsync_WithoutDocumentId_ShouldReturnAllAccessible()
    {
        var user = Context.Users.First();
        var permissions = new List<string>(); // без права ViewAnyDocument
        var result = await _service.GetAllAsync(null, user.Id, permissions, CancellationToken.None);
        result.Should().HaveCount(3); // все документы принадлежат user
    }

    [Fact]
    public async Task GetAllAsync_WithDocumentId_ShouldReturnVersions()
    {
        var user = Context.Users.First();
        var doc = Context.Documents.First();
        var permissions = new List<string>();
        var result = await _service.GetAllAsync(doc.Id, user.Id, permissions, CancellationToken.None);
        result.Should().HaveCount(2); // у doc1 две версии
    }

    [Fact]
    public async Task GetAllAsync_WithDocumentIdButNoAccess_ShouldThrow()
    {
        var otherUser = new User { Id = Guid.NewGuid(), Username = "other", Email = "o@test.com" };
        Context.Users.Add(otherUser);
        await Context.SaveChangesAsync();
        var doc = Context.Documents.First();
        var permissions = new List<string>();
        await FluentActions
            .Invoking(() => _service.GetAllAsync(doc.Id, otherUser.Id, permissions, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GetByIdAsync_Existing_ShouldReturn()
    {
        var user = Context.Users.First();
        var version = Context.DocumentVersions.First();
        var permissions = new List<string>();
        var result = await _service.GetByIdAsync(version.Id, user.Id, permissions, default);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_WithPermission_ShouldCreate()
    {
        var user = Context.Users.First();
        var doc = Context.Documents.First();
        var permissions = new List<string> { "EditOwnDocuments" };
        var dto = new CreateDocumentVersionDto
        {
            DocumentId = doc.Id,
            VersionNumber = 3,
            FileName = "v3.pdf",
            FileSize = 1024,
            Comment = "new version"
        };
        var result = await _service.CreateAsync(dto, user.Id, permissions, default);
        result.Id.Should().NotBeEmpty();
        Context.DocumentVersions.Count().Should().Be(4);
    }

    [Fact]
    public async Task CreateAsync_DuplicateVersion_ShouldThrow()
    {
        var user = Context.Users.First();
        var doc = Context.Documents.First();
        var permissions = new List<string> { "EditOwnDocuments" };
        var dto = new CreateDocumentVersionDto
        {
            DocumentId = doc.Id,
            VersionNumber = 1, // уже существует
            FileName = "dup.pdf",
            FileSize = 1024
        };
        await FluentActions.Invoking(() => _service.CreateAsync(dto, user.Id, permissions, default))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateComment()
    {
        var user = Context.Users.First();
        var version = Context.DocumentVersions.First(v => v.Document.UserId == user.Id);
        var permissions = new List<string> { "EditOwnDocuments" };
        var dto = new UpdateDocumentVersionDto { Comment = "Updated comment" };
        await _service.UpdateAsync(version.Id, dto, user.Id, permissions, default);
        var updated = await Context.DocumentVersions.FindAsync(version.Id);
        updated!.Comment.Should().Be("Updated comment");
    }

    [Fact]
    public async Task DeleteAsync_WithPermission_ShouldDelete()
    {
        var user = Context.Users.First();
        var version = Context.DocumentVersions.First(v => v.Document.UserId == user.Id);
        var permissions = new List<string> { "DeleteOwnDocuments" };
        await _service.DeleteAsync(version.Id, user.Id, permissions, default);
        Context.DocumentVersions.Any(v => v.Id == version.Id).Should().BeFalse();
    }
}