using DocumentArchive.Core.DTOs.Document;
using DocumentArchive.Core.Models;
using DocumentArchive.Infrastructure.Repositories;
using DocumentArchive.Services.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;

namespace DocumentArchive.Tests.ValidatorsTests;

public class CreateDocumentDtoValidatorTests
{
    private readonly Mock<CategoryRepository> _categoryRepoMock;
    private readonly Mock<UserRepository> _userRepoMock;
    private readonly CreateDocumentDtoValidator _validator;

    public CreateDocumentDtoValidatorTests()
    {
        _categoryRepoMock = new Mock<CategoryRepository>();
        _userRepoMock = new Mock<UserRepository>();
        _validator = new CreateDocumentDtoValidator(_categoryRepoMock.Object, _userRepoMock.Object);
    }

    [Fact]
    public async Task Should_HaveError_When_Title_IsEmpty()
    {
        // Arrange
        var dto = new CreateDocumentDto { Title = "", FileName = "test.pdf" };

        // Act
        var result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage("Название документа обязательно");
    }

    [Fact]
    public async Task Should_HaveError_When_Title_ExceedsMaxLength()
    {
        // Arrange
        var dto = new CreateDocumentDto { Title = new string('a', 201), FileName = "test.pdf" };

        // Act
        var result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage("Название не должно превышать 200 символов");
    }

    [Fact]
    public async Task Should_HaveError_When_FileName_IsEmpty()
    {
        // Arrange
        var dto = new CreateDocumentDto { Title = "Title", FileName = "" };

        // Act
        var result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FileName)
            .WithErrorMessage("Имя файла обязательно");
    }

    [Fact]
    public async Task Should_HaveError_When_FileName_ExceedsMaxLength()
    {
        // Arrange
        var dto = new CreateDocumentDto { Title = "Title", FileName = new string('a', 101) };

        // Act
        var result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FileName)
            .WithErrorMessage("Имя файла не должно превышать 100 символов");
    }

    [Fact]
    public async Task Should_HaveError_When_CategoryId_DoesNotExist()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        _categoryRepoMock.Setup(x => x.GetByIdAsync(categoryId))
            .ReturnsAsync((Category?)null);
        var dto = new CreateDocumentDto
        {
            Title = "Title",
            FileName = "test.pdf",
            CategoryId = categoryId
        };

        // Act
        var result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CategoryId)
            .WithErrorMessage("Категория с указанным ID не существует");
    }

    [Fact]
    public async Task Should_NotHaveError_When_CategoryId_Exists()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        _categoryRepoMock.Setup(x => x.GetByIdAsync(categoryId))
            .ReturnsAsync(new Category { Id = categoryId });
        var dto = new CreateDocumentDto
        {
            Title = "Title",
            FileName = "test.pdf",
            CategoryId = categoryId
        };

        // Act
        var result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CategoryId);
    }

    [Fact]
    public async Task Should_NotHaveError_When_CategoryId_IsNull()
    {
        // Arrange
        var dto = new CreateDocumentDto
        {
            Title = "Title",
            FileName = "test.pdf",
            CategoryId = null
        };

        // Act
        var result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CategoryId);
    }

    [Fact]
    public async Task Should_HaveError_When_UserId_DoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepoMock.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);
        var dto = new CreateDocumentDto
        {
            Title = "Title",
            FileName = "test.pdf",
            UserId = userId
        };

        // Act
        var result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage("Пользователь с указанным ID не существует");
    }
}