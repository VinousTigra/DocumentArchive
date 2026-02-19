using DocumentArchive.Core.DTOs.Role;
using DocumentArchive.Services.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace DocumentArchive.Tests.ValidatorsTests;

public class UpdateRoleDtoValidatorTests
{
    private readonly UpdateRoleDtoValidator _validator;

    public UpdateRoleDtoValidatorTests()
    {
        _validator = new UpdateRoleDtoValidator();
    }

    [Fact]
    public async Task Should_NotHaveError_When_Name_IsNull()
    {
        var dto = new UpdateRoleDto { Name = null };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Should_HaveError_When_Name_ExceedsMaxLength()
    {
        var dto = new UpdateRoleDto { Name = new string('a', 51) };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Should_NotHaveError_When_Description_IsNull()
    {
        var dto = new UpdateRoleDto { Description = null };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public async Task Should_HaveError_When_Description_ExceedsMaxLength()
    {
        var dto = new UpdateRoleDto { Description = new string('a', 201) };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }
}