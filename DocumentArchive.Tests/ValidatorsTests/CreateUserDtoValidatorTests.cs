using DocumentArchive.Core.DTOs.User;
using DocumentArchive.Core.Models;
using DocumentArchive.Infrastructure.Repositories;
using DocumentArchive.Services.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;

namespace DocumentArchive.Tests.ValidatorsTests;

public class CreateUserDtoValidatorTests
{
    private readonly Mock<UserRepository> _userRepoMock;
    private readonly CreateUserDtoValidator _validator;

    public CreateUserDtoValidatorTests()
    {
        _userRepoMock = new Mock<UserRepository>();
        _validator = new CreateUserDtoValidator(_userRepoMock.Object);
    }

    [Fact]
    public async Task Should_HaveError_When_Username_IsEmpty()
    {
        // Arrange
        var dto = new CreateUserDto { Username = "", Email = "test@test.com" };

        // Act
        var result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public async Task Should_HaveError_When_Email_IsInvalid()
    {
        // Arrange
        var dto = new CreateUserDto { Username = "user", Email = "not-an-email" };

        // Act
        var result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public async Task Should_HaveError_When_Email_AlreadyExists()
    {
        // Arrange
        var dto = new CreateUserDto { Username = "user", Email = "exists@test.com" };
        _userRepoMock.Setup(x => x.FindByEmailAsync("exists@test.com"))
            .ReturnsAsync(new User { Email = "exists@test.com" });

        // Act
        var result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Пользователь с таким email уже существует");
    }

    [Fact]
    public async Task Should_NotHaveError_When_Email_IsUnique()
    {
        // Arrange
        var dto = new CreateUserDto { Username = "user", Email = "new@test.com" };
        _userRepoMock.Setup(x => x.FindByEmailAsync("new@test.com"))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }
}