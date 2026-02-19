using DocumentArchive.Core.DTOs.Permission;
using DocumentArchive.Core.Models;
using DocumentArchive.Services.Validators;
using DocumentArchive.Tests.ServicesTests;
using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;

namespace DocumentArchive.Tests.ValidatorsTests;

public class CreatePermissionDtoValidatorTests : TestBase
{
    private readonly CreatePermissionDtoValidator _validator;

    public CreatePermissionDtoValidatorTests()
    {
        _validator = new CreatePermissionDtoValidator(Context);
    }

    [Fact]
    public async Task Should_HaveError_When_Name_IsEmpty()
    {
        var dto = new CreatePermissionDto { Name = "" };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Should_HaveError_When_Name_ExceedsMaxLength()
    {
        var dto = new CreatePermissionDto { Name = new string('a', 101) };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Should_HaveError_When_Name_AlreadyExists()
    {
        // Arrange
        Context.Permissions.Add(new Permission { Name = "Existing" });
        await Context.SaveChangesAsync();
        var dto = new CreatePermissionDto { Name = "Existing" };

        // Act
        var result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Permission with this name already exists.");
    }

    [Fact]
    public async Task Should_NotHaveError_When_Name_IsUnique()
    {
        var dto = new CreatePermissionDto { Name = "UniquePerm" };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Should_NotHaveError_When_Description_IsNull()
    {
        var dto = new CreatePermissionDto { Name = "Perm", Description = null };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public async Task Should_HaveError_When_Description_ExceedsMaxLength()
    {
        var dto = new CreatePermissionDto { Name = "Perm", Description = new string('a', 201) };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public async Task Should_NotHaveError_When_Category_IsNull()
    {
        var dto = new CreatePermissionDto { Name = "Perm", Category = null };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Category);
    }

    [Fact]
    public async Task Should_HaveError_When_Category_ExceedsMaxLength()
    {
        var dto = new CreatePermissionDto { Name = "Perm", Category = new string('a', 51) };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Category);
    }
}