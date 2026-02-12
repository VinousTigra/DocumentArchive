using DocumentArchive.Core.DTOs.Category;
using DocumentArchive.Core.Interfaces;
using DocumentArchive.Core.Models;
using DocumentArchive.Services.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;

namespace DocumentArchive.Tests.ValidatorsTests;

public class CreateCategoryDtoValidatorTests
{
    private readonly Mock<ICategoryRepository> _categoryRepoMock;
    private readonly CreateCategoryDtoValidator _validator;

    public CreateCategoryDtoValidatorTests()
    {
        _categoryRepoMock = new Mock<ICategoryRepository>();
        _validator = new CreateCategoryDtoValidator(_categoryRepoMock.Object);
    }

    [Fact]
    public async Task Should_HaveError_When_Name_IsEmpty()
    {
        var dto = new CreateCategoryDto { Name = "" };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Should_HaveError_When_Name_ExceedsMaxLength()
    {
        var dto = new CreateCategoryDto { Name = new string('a', 101) };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Should_HaveError_When_Name_AlreadyExists()
    {
        var dto = new CreateCategoryDto { Name = "HR" };
        _categoryRepoMock.Setup(x => x.FindByNameAsync(dto.Name))
            .ReturnsAsync(new Category { Name = dto.Name });
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Should_NotHaveError_When_Name_IsUnique()
    {
        var dto = new CreateCategoryDto { Name = "NewCategory" };
        _categoryRepoMock.Setup(x => x.FindByNameAsync(dto.Name))
            .ReturnsAsync((Category?)null);
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }
}