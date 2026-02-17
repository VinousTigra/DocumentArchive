using DocumentArchive.Core.DTOs.Document;
using DocumentArchive.Core.Interfaces.Repositorys;
using DocumentArchive.Core.Models;
using DocumentArchive.Services.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;

namespace DocumentArchive.Tests.ValidatorsTests;

public class CreateDocumentDtoValidatorTests
{
    private readonly Mock<ICategoryRepository> _categoryRepoMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly CreateDocumentDtoValidator _validator;

    public CreateDocumentDtoValidatorTests()
    {
        _categoryRepoMock = new Mock<ICategoryRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _validator = new CreateDocumentDtoValidator(
            _categoryRepoMock.Object,
            _userRepoMock.Object);
    }

    [Fact]
    public async Task Should_HaveError_When_Title_IsEmpty()
    {
        var dto = new CreateDocumentDto { Title = "", FileName = "test.pdf" };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public async Task Should_HaveError_When_Title_ExceedsMaxLength()
    {
        var dto = new CreateDocumentDto { Title = new string('a', 201), FileName = "test.pdf" };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public async Task Should_HaveError_When_FileName_IsEmpty()
    {
        var dto = new CreateDocumentDto { Title = "Title", FileName = "" };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.FileName);
    }

    [Fact]
    public async Task Should_HaveError_When_FileName_ExceedsMaxLength()
    {
        var dto = new CreateDocumentDto { Title = "Title", FileName = new string('a', 101) };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.FileName);
    }

    [Fact]
    public async Task Should_HaveError_When_CategoryId_DoesNotExist()
    {
        var categoryId = Guid.NewGuid();
        _categoryRepoMock.Setup(x => x.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);
        var dto = new CreateDocumentDto
        {
            Title = "Title",
            FileName = "test.pdf",
            CategoryId = categoryId
        };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.CategoryId);
    }

    [Fact]
    public async Task Should_NotHaveError_When_CategoryId_Exists()
    {
        var categoryId = Guid.NewGuid();
        _categoryRepoMock.Setup(x => x.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Category { Id = categoryId });
        var dto = new CreateDocumentDto
        {
            Title = "Title",
            FileName = "test.pdf",
            CategoryId = categoryId
        };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.CategoryId);
    }

    [Fact]
    public async Task Should_HaveError_When_UserId_DoesNotExist()
    {
        var userId = Guid.NewGuid();
        _userRepoMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        var dto = new CreateDocumentDto
        {
            Title = "Title",
            FileName = "test.pdf",
            UserId = userId
        };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public async Task Should_NotHaveError_When_UserId_Exists()
    {
        var userId = Guid.NewGuid();
        _userRepoMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = userId });
        var dto = new CreateDocumentDto
        {
            Title = "Title",
            FileName = "test.pdf",
            UserId = userId
        };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.UserId);
    }
}