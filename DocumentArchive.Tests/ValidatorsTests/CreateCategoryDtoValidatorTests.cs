using DocumentArchive.Core.DTOs.Category;
using DocumentArchive.Core.Models;
using DocumentArchive.Infrastructure.Repositories;
using DocumentArchive.Services.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;

namespace DocumentArchive.Tests.ValidatorsTests;

public class CreateCategoryDtoValidatorTests
{
    private readonly Mock<CategoryRepository> _categoryRepoMock;
    private readonly CreateCategoryDtoValidator _validator;

    public CreateCategoryDtoValidatorTests()
    {
        _categoryRepoMock = new Mock<CategoryRepository>();
        _validator = new CreateCategoryDtoValidator(_categoryRepoMock.Object);
    }

    [Fact]
    public async Task Should_HaveError_When_Name_IsEmpty()
    {
        // Arrange
        var dto = new CreateCategoryDto { Name = "" };

        // Act
        var result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Should_HaveError_When_Name_AlreadyExists()
    {
        // Arrange
        var dto = new CreateCategoryDto { Name = "HR" };
        _categoryRepoMock.Setup(x => x.FindByNameAsync("HR"))
            .ReturnsAsync(new Category { Name = "HR" });

        // Act
        var result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Категория с таким названием уже существует");
    }
}