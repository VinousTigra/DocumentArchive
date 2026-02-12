using DocumentArchive.Core.DTOs.Category;
using DocumentArchive.Core.Interfaces;
using DocumentArchive.Services.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;

namespace DocumentArchive.Tests.ValidatorsTests;

public class UpdateCategoryDtoValidatorTests
{
    private readonly Mock<ICategoryRepository> _categoryRepoMock;
    private readonly UpdateCategoryDtoValidator _validator;

    public UpdateCategoryDtoValidatorTests()
    {
        _categoryRepoMock = new Mock<ICategoryRepository>();
        _validator = new UpdateCategoryDtoValidator(_categoryRepoMock.Object);
    }

    [Fact]
    public async Task Should_HaveError_When_Name_ExceedsMaxLength()
    {
        var dto = new UpdateCategoryDto { Name = new string('a', 101) };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Should_HaveError_When_Description_ExceedsMaxLength()
    {
        var dto = new UpdateCategoryDto { Description = new string('a', 501) };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }
}