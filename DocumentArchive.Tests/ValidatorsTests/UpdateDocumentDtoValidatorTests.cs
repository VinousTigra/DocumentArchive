using DocumentArchive.Core.DTOs.Document;
using DocumentArchive.Core.Models;
using DocumentArchive.Infrastructure.Repositories;
using DocumentArchive.Services.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;

namespace DocumentArchive.Tests.ValidatorsTests;

public class UpdateDocumentDtoValidatorTests
{
    private readonly Mock<CategoryRepository> _categoryRepoMock;
    private readonly UpdateDocumentDtoValidator _validator;

    public UpdateDocumentDtoValidatorTests()
    {
        _categoryRepoMock = new Mock<CategoryRepository>();
        _validator = new UpdateDocumentDtoValidator(_categoryRepoMock.Object);
    }

    [Fact]
    public async Task Should_NotHaveError_When_Title_IsNull()
    {
        // Arrange
        var dto = new UpdateDocumentDto { Title = null };

        // Act
        var result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public async Task Should_HaveError_When_Title_ExceedsMaxLength()
    {
        // Arrange
        var dto = new UpdateDocumentDto { Title = new string('a', 201) };

        // Act
        var result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage("Название не должно превышать 200 символов");
    }

    [Fact]
    public async Task Should_HaveError_When_CategoryId_DoesNotExist()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        _categoryRepoMock.Setup(x => x.GetByIdAsync(categoryId))
            .ReturnsAsync((Category?)null);
        var dto = new UpdateDocumentDto { CategoryId = categoryId };

        // Act
        var result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CategoryId)
            .WithErrorMessage("Категория с указанным ID не существует");
    }
}