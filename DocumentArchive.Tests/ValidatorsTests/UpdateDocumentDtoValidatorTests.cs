using DocumentArchive.Core.DTOs.Document;
using DocumentArchive.Core.Interfaces;
using DocumentArchive.Core.Models;
using DocumentArchive.Services.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;

namespace DocumentArchive.Tests.ValidatorsTests;

public class UpdateDocumentDtoValidatorTests
{
    private readonly Mock<ICategoryRepository> _categoryRepoMock;
    private readonly UpdateDocumentDtoValidator _validator;

    public UpdateDocumentDtoValidatorTests()
    {
        _categoryRepoMock = new Mock<ICategoryRepository>();
        _validator = new UpdateDocumentDtoValidator(_categoryRepoMock.Object);
    }

    [Fact]
    public async Task Should_HaveError_When_Title_ExceedsMaxLength()
    {
        var dto = new UpdateDocumentDto { Title = new string('a', 201) };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public async Task Should_HaveError_When_FileName_ExceedsMaxLength()
    {
        var dto = new UpdateDocumentDto { FileName = new string('a', 101) };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.FileName);
    }

    [Fact]
    public async Task Should_HaveError_When_CategoryId_DoesNotExist()
    {
        var categoryId = Guid.NewGuid();
        _categoryRepoMock.Setup(x => x.GetByIdAsync(categoryId))
            .ReturnsAsync((Category?)null);
        var dto = new UpdateDocumentDto { CategoryId = categoryId };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.CategoryId);
    }

    [Fact]
    public async Task Should_NotHaveError_When_CategoryId_Exists()
    {
        var categoryId = Guid.NewGuid();
        _categoryRepoMock.Setup(x => x.GetByIdAsync(categoryId))
            .ReturnsAsync(new Category { Id = categoryId });
        var dto = new UpdateDocumentDto { CategoryId = categoryId };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.CategoryId);
    }
}