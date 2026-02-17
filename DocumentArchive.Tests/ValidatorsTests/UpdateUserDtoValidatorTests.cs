using DocumentArchive.Core.DTOs.User;
using DocumentArchive.Core.Interfaces.Repositorys;
using DocumentArchive.Services.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;

namespace DocumentArchive.Tests.ValidatorsTests;

public class UpdateUserDtoValidatorTests
{
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly UpdateUserDtoValidator _validator;

    public UpdateUserDtoValidatorTests()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _validator = new UpdateUserDtoValidator(_userRepoMock.Object);
    }

    [Fact]
    public async Task Should_HaveError_When_Username_ExceedsMaxLength()
    {
        var dto = new UpdateUserDto { Username = new string('a', 51) };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public async Task Should_HaveError_When_Email_IsInvalid()
    {
        var dto = new UpdateUserDto { Email = "invalid" };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }
}