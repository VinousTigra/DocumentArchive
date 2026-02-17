using DocumentArchive.Core.DTOs.User;
using DocumentArchive.Core.Interfaces.Repositorys;
using DocumentArchive.Core.Models;
using DocumentArchive.Services.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;

namespace DocumentArchive.Tests.ValidatorsTests;

public class CreateUserDtoValidatorTests
{
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly CreateUserDtoValidator _validator;

    public CreateUserDtoValidatorTests()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _validator = new CreateUserDtoValidator(_userRepoMock.Object);
    }

    [Fact]
    public async Task Should_HaveError_When_Username_IsEmpty()
    {
        var dto = new CreateUserDto { Username = "", Email = "test@test.com" };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public async Task Should_HaveError_When_Email_IsInvalid()
    {
        var dto = new CreateUserDto { Username = "user", Email = "invalid" };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public async Task Should_HaveError_When_Email_AlreadyExists()
    {
        var dto = new CreateUserDto { Username = "user", Email = "exists@test.com" };
        _userRepoMock.Setup(x => x.FindByEmailAsync(dto.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Email = dto.Email });
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public async Task Should_NotHaveError_When_Email_IsUnique()
    {
        var dto = new CreateUserDto { Username = "user", Email = "new@test.com" };
        _userRepoMock.Setup(x => x.FindByEmailAsync(dto.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }
}