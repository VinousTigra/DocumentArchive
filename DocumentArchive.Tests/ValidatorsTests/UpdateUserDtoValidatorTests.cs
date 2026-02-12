using DocumentArchive.Core.DTOs.User;
using DocumentArchive.Infrastructure.Repositories;
using DocumentArchive.Services.Validators;
using FluentValidation.TestHelper;
using Moq;

namespace DocumentArchive.Tests.ValidatorsTests;

public class UpdateUserDtoValidatorTests
{
    private readonly Mock<UserRepository> _userRepoMock;
    private readonly UpdateUserDtoValidator _validator;

    public UpdateUserDtoValidatorTests()
    {
        _userRepoMock = new Mock<UserRepository>();
        _validator = new UpdateUserDtoValidator(_userRepoMock.Object);
    }

    [Fact]
    public async Task Should_NotHaveError_When_Username_IsNull()
    {
        // Arrange
        var dto = new UpdateUserDto { Username = null };

        // Act
        var result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public async Task Should_HaveError_When_Username_ExceedsMaxLength()
    {
        // Arrange
        var dto = new UpdateUserDto { Username = new string('a', 51) };

        // Act
        var result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public async Task Should_HaveError_When_Email_IsInvalid()
    {
        // Arrange
        var dto = new UpdateUserDto { Email = "invalid" };

        // Act
        var result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }
}