using AutoMapper;
using DocumentArchive.Core.DTOs.DocumentVersion;
using DocumentArchive.Core.Models;
using DocumentArchive.Services.Mapping;
using DocumentArchive.Services.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace DocumentArchive.Tests.ServicesTests;

public class DocumentVersionServiceTests : TestBase
{
    private readonly DocumentVersionService _service;
    private readonly IMapper _mapper;

    public DocumentVersionServiceTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();
        _service = new DocumentVersionService(Context, _mapper, NullLogger<DocumentVersionService>.Instance);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllVersions()
    {
        // Arrange
        var doc = new Document { Id = Guid.NewGuid(), Title = "Doc" };
        Context.Documents.Add(doc);
        await Context.SaveChangesAsync();

        var versions = new[]
        {
            new DocumentVersion { DocumentId = doc.Id, VersionNumber = 1, FileName = "v1.pdf", FileSize = 100 },
            new DocumentVersion { DocumentId = doc.Id, VersionNumber = 2, FileName = "v2.pdf", FileSize = 200 }
        };
        Context.DocumentVersions.AddRange(versions);
        await Context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllAsync(null, default);

        // Assert
        result.Should().HaveCount(2);
        result.Select(v => v.VersionNumber).Should().Contain(new[] { 1, 2 });
    }

    [Fact]
    public async Task GetAllAsync_WithDocumentId_ShouldFilterByDocument()
    {
        // Arrange
        var doc1 = new Document { Id = Guid.NewGuid(), Title = "Doc1" };
        var doc2 = new Document { Id = Guid.NewGuid(), Title = "Doc2" };
        Context.Documents.AddRange(doc1, doc2);
        await Context.SaveChangesAsync();

        Context.DocumentVersions.AddRange(
            new DocumentVersion { DocumentId = doc1.Id, VersionNumber = 1, FileName = "v1.pdf", FileSize = 100 },
            new DocumentVersion { DocumentId = doc2.Id, VersionNumber = 1, FileName = "v1.pdf", FileSize = 100 }
        );
        await Context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllAsync(doc1.Id, default);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnVersion_WhenExists()
    {
        // Arrange
        var doc = new Document { Id = Guid.NewGuid(), Title = "Doc" };
        Context.Documents.Add(doc);
        await Context.SaveChangesAsync();

        var version = new DocumentVersion
        {
            DocumentId = doc.Id,
            VersionNumber = 1,
            FileName = "v1.pdf",
            FileSize = 100,
            Comment = "Initial"
        };
        Context.DocumentVersions.Add(version);
        await Context.SaveChangesAsync();

        // Act
        var result = await _service.GetByIdAsync(version.Id, default);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(version.Id);
        result.VersionNumber.Should().Be(1);
        result.FileName.Should().Be("v1.pdf");
        result.Comment.Should().Be("Initial");
        result.DocumentId.Should().Be(doc.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = await _service.GetByIdAsync(Guid.NewGuid(), default);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ShouldAddDocumentVersion()
    {
        // Arrange
        var doc = new Document { Id = Guid.NewGuid(), Title = "Doc" };
        Context.Documents.Add(doc);
        await Context.SaveChangesAsync();

        var dto = new CreateDocumentVersionDto
        {
            DocumentId = doc.Id,
            VersionNumber = 1,
            FileName = "v1.pdf",
            FileSize = 1024,
            Comment = "First version",
            UploadedBy = Guid.NewGuid()
        };

        // Act
        var result = await _service.CreateAsync(dto, default);

        // Assert
        result.Should().NotBeNull();
        result.VersionNumber.Should().Be(1);
        result.FileName.Should().Be("v1.pdf");
        result.Comment.Should().Be("First version");

        var version = await Context.DocumentVersions.FirstOrDefaultAsync(v =>
            v.DocumentId == doc.Id && v.VersionNumber == 1);
        version.Should().NotBeNull();
        version!.UploadedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenDocumentNotFound()
    {
        // Arrange
        var dto = new CreateDocumentVersionDto
        {
            DocumentId = Guid.NewGuid(),
            VersionNumber = 1,
            FileName = "v1.pdf",
            FileSize = 1024
        };

        // Act
        Func<Task> act = async () => await _service.CreateAsync(dto, default);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Document with id {dto.DocumentId} not found.");
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenVersionNumberAlreadyExists()
    {
        // Arrange
        var doc = new Document { Id = Guid.NewGuid(), Title = "Doc" };
        Context.Documents.Add(doc);
        await Context.SaveChangesAsync();

        Context.DocumentVersions.Add(new DocumentVersion
        {
            DocumentId = doc.Id,
            VersionNumber = 1,
            FileName = "existing.pdf",
            FileSize = 100
        });
        await Context.SaveChangesAsync();

        var dto = new CreateDocumentVersionDto
        {
            DocumentId = doc.Id,
            VersionNumber = 1,
            FileName = "new.pdf",
            FileSize = 200
        };

        // Act
        Func<Task> act = async () => await _service.CreateAsync(dto, default);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Version number 1 already exists for this document.");
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateComment()
    {
        // Arrange
        var doc = new Document { Id = Guid.NewGuid(), Title = "Doc" };
        Context.Documents.Add(doc);
        await Context.SaveChangesAsync();

        var version = new DocumentVersion
        {
            DocumentId = doc.Id,
            VersionNumber = 1,
            FileName = "v1.pdf",
            FileSize = 100,
            Comment = "Old comment"
        };
        Context.DocumentVersions.Add(version);
        await Context.SaveChangesAsync();

        var updateDto = new UpdateDocumentVersionDto { Comment = "Updated comment" };

        // Act
        await _service.UpdateAsync(version.Id, updateDto, default);

        // Assert
        var updated = await Context.DocumentVersions.FindAsync(version.Id);
        updated!.Comment.Should().Be("Updated comment");
        updated.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrow_WhenVersionNotFound()
    {
        // Act
        Func<Task> act = async () => await _service.UpdateAsync(Guid.NewGuid(), new UpdateDocumentVersionDto(), default);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveVersion()
    {
        // Arrange
        var doc = new Document { Id = Guid.NewGuid(), Title = "Doc" };
        Context.Documents.Add(doc);
        await Context.SaveChangesAsync();

        var version = new DocumentVersion
        {
            DocumentId = doc.Id,
            VersionNumber = 1,
            FileName = "v1.pdf",
            FileSize = 100
        };
        Context.DocumentVersions.Add(version);
        await Context.SaveChangesAsync();

        // Act
        await _service.DeleteAsync(version.Id, default);

        // Assert
        var deleted = await Context.DocumentVersions.FindAsync(version.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrow_WhenVersionNotFound()
    {
        // Act
        Func<Task> act = async () => await _service.DeleteAsync(Guid.NewGuid(), default);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}