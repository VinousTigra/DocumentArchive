using DocumentArchive.Core.DTOs.Role;
using DocumentArchive.Core.Models;
using DocumentArchive.Services.Validators;
using DocumentArchive.Tests.ServicesTests;
using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;

namespace DocumentArchive.Tests.ValidatorsTests;

public class CreateRoleDtoValidatorTests : TestBase
{
    private readonly CreateRoleDtoValidator _validator;

    public CreateRoleDtoValidatorTests()
    {
        _validator = new CreateRoleDtoValidator(Context);
    }

    [Fact]
    public async Task Should_HaveError_When_Name_IsEmpty()
    {
        var dto = new CreateRoleDto { Name = "" };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Should_HaveError_When_Name_ExceedsMaxLength()
    {
        var dto = new CreateRoleDto { Name = new string('a', 51) };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Should_HaveError_When_Name_AlreadyExists()
    {
        // Arrange
        Context.Roles.Add(new Role { Name = "Existing" });
        await Context.SaveChangesAsync();
        var dto = new CreateRoleDto { Name = "Existing" };

        // Act
        var result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Role with this name already exists.");
    }

    [Fact]
    public async Task Should_NotHaveError_When_Name_IsUnique()
    {
        var dto = new CreateRoleDto { Name = "UniqueRole" };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Should_NotHaveError_When_Description_IsNull()
    {
        var dto = new CreateRoleDto { Name = "Role", Description = null };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public async Task Should_HaveError_When_Description_ExceedsMaxLength()
    {
        var dto = new CreateRoleDto { Name = "Role", Description = new string('a', 201) };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }
}