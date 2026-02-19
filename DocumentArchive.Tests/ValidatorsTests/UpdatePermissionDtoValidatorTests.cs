using DocumentArchive.Core.DTOs.Permission;
using DocumentArchive.Services.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace DocumentArchive.Tests.ValidatorsTests;

public class UpdatePermissionDtoValidatorTests
{
    private readonly UpdatePermissionDtoValidator _validator;

    public UpdatePermissionDtoValidatorTests()
    {
        _validator = new UpdatePermissionDtoValidator();
    }

    [Fact]
    public async Task Should_NotHaveError_When_Name_IsNull()
    {
        var dto = new UpdatePermissionDto { Name = null };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Should_HaveError_When_Name_ExceedsMaxLength()
    {
        var dto = new UpdatePermissionDto { Name = new string('a', 101) };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Should_NotHaveError_When_Description_IsNull()
    {
        var dto = new UpdatePermissionDto { Description = null };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public async Task Should_HaveError_When_Description_ExceedsMaxLength()
    {
        var dto = new UpdatePermissionDto { Description = new string('a', 201) };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public async Task Should_NotHaveError_When_Category_IsNull()
    {
        var dto = new UpdatePermissionDto { Category = null };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Category);
    }

    [Fact]
    public async Task Should_HaveError_When_Category_ExceedsMaxLength()
    {
        var dto = new UpdatePermissionDto { Category = new string('a', 51) };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Category);
    }
}