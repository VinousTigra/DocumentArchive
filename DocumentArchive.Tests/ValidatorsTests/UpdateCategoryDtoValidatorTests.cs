using DocumentArchive.Core.DTOs.Category;
using DocumentArchive.Infrastructure.Repositories;
using DocumentArchive.Services.Validators;
using FluentValidation.TestHelper;
using Moq;

namespace DocumentArchive.Tests.ValidatorsTests;

public class UpdateCategoryDtoValidatorTests
{
    private readonly Mock<CategoryRepository> _categoryRepoMock;
    private readonly UpdateCategoryDtoValidator _validator;

    public UpdateCategoryDtoValidatorTests()
    {
        _categoryRepoMock = new Mock<CategoryRepository>();
        _validator = new UpdateCategoryDtoValidator(_categoryRepoMock.Object);
    }

    [Fact]
    public async Task Should_NotHaveError_When_Name_IsNull()
    {
        // Arrange
        var dto = new UpdateCategoryDto { Name = null };

        // Act
        var result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Should_HaveError_When_Name_ExceedsMaxLength()
    {
        // Arrange
        var dto = new UpdateCategoryDto { Name = new string('a', 101) };

        // Act
        var result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }
}