using DocumentArchive.Core.DTOs.Auth;
using FluentValidation.TestHelper;

namespace DocumentArchive.Tests.ValidatorsTests;

public class RefreshTokenDtoValidatorTests
{
    private readonly RefreshTokenDtoValidator _validator;

    public RefreshTokenDtoValidatorTests()
    {
        _validator = new RefreshTokenDtoValidator();
    }

    [Fact]
    public void Should_Have_Error_When_AccessToken_Is_Empty()
    {
        // Arrange
        var dto = new RefreshTokenDto
        {
            AccessToken = "",
            RefreshToken = "valid_refresh_token"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AccessToken)
            .WithErrorMessage("Access token is required");
    }

    [Fact]
    public void Should_Have_Error_When_RefreshToken_Is_Empty()
    {
        // Arrange
        var dto = new RefreshTokenDto
        {
            AccessToken = "valid_access_token",
            RefreshToken = ""
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RefreshToken)
            .WithErrorMessage("Refresh token is required");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Both_Tokens_Are_Provided()
    {
        // Arrange
        var dto = new RefreshTokenDto
        {
            AccessToken = "valid_access_token",
            RefreshToken = "valid_refresh_token"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Errors_For_Both_Fields_When_Both_Are_Empty()
    {
        // Arrange
        var dto = new RefreshTokenDto
        {
            AccessToken = "",
            RefreshToken = ""
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AccessToken);
        result.ShouldHaveValidationErrorFor(x => x.RefreshToken);
    }
}