using DocumentArchive.Core.DTOs.DocumentVersion;
using DocumentArchive.Core.Models;
using DocumentArchive.Services.Validators;
using DocumentArchive.Tests.ServicesTests;
using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;

namespace DocumentArchive.Tests.ValidatorsTests;

public class CreateDocumentVersionDtoValidatorTests : TestBase
{
    private readonly CreateDocumentVersionDtoValidator _validator;

    public CreateDocumentVersionDtoValidatorTests()
    {
        _validator = new CreateDocumentVersionDtoValidator(Context);
    }

    [Fact]
    public async Task Should_HaveError_When_DocumentId_IsEmpty()
    {
        var dto = new CreateDocumentVersionDto { DocumentId = Guid.Empty };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.DocumentId);
    }

    [Fact]
    public async Task Should_HaveError_When_DocumentId_DoesNotExist()
    {
        var dto = new CreateDocumentVersionDto { DocumentId = Guid.NewGuid() };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.DocumentId)
            .WithErrorMessage("Document with this ID does not exist.");
    }

    [Fact]
    public async Task Should_NotHaveError_When_DocumentId_Exists()
    {
        // Arrange
        var doc = new Document { Id = Guid.NewGuid(), Title = "Test" };
        Context.Documents.Add(doc);
        await Context.SaveChangesAsync();
        var dto = new CreateDocumentVersionDto { DocumentId = doc.Id };

        // Act
        var result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DocumentId);
    }

    [Fact]
    public async Task Should_HaveError_When_VersionNumber_IsZeroOrNegative()
    {
        var dto = new CreateDocumentVersionDto { VersionNumber = 0 };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.VersionNumber);
    }

    [Fact]
    public async Task Should_HaveError_When_VersionNumber_AlreadyExists()
    {
        // Arrange
        var doc = new Document { Id = Guid.NewGuid(), Title = "Doc" };
        Context.Documents.Add(doc);
        await Context.SaveChangesAsync();
        Context.DocumentVersions.Add(new DocumentVersion
        {
            DocumentId = doc.Id,
            VersionNumber = 1,
            FileName = "test.pdf",
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
        var result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.VersionNumber)
            .WithErrorMessage("Version number already exists for this document.");
    }

    [Fact]
    public async Task Should_NotHaveError_When_VersionNumber_IsUnique()
    {
        // Arrange
        var doc = new Document { Id = Guid.NewGuid(), Title = "Doc" };
        Context.Documents.Add(doc);
        await Context.SaveChangesAsync();

        var dto = new CreateDocumentVersionDto
        {
            DocumentId = doc.Id,
            VersionNumber = 1,
            FileName = "new.pdf",
            FileSize = 200
        };

        // Act
        var result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.VersionNumber);
    }

    [Fact]
    public async Task Should_HaveError_When_FileName_IsEmpty()
    {
        var dto = new CreateDocumentVersionDto { FileName = "" };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.FileName);
    }

    [Fact]
    public async Task Should_HaveError_When_FileName_ExceedsMaxLength()
    {
        var dto = new CreateDocumentVersionDto { FileName = new string('a', 256) };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.FileName);
    }

    [Fact]
    public async Task Should_HaveError_When_FileSize_IsZeroOrNegative()
    {
        var dto = new CreateDocumentVersionDto { FileSize = 0 };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.FileSize);
    }

    [Fact]
    public async Task Should_HaveError_When_Comment_ExceedsMaxLength()
    {
        var dto = new CreateDocumentVersionDto { Comment = new string('a', 501) };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Comment);
    }

    [Fact]
    public async Task Should_NotHaveError_When_Comment_IsNull()
    {
        var dto = new CreateDocumentVersionDto { Comment = null };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Comment);
    }
}