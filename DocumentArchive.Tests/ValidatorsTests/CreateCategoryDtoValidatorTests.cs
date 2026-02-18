using DocumentArchive.Core.DTOs.Category;
using DocumentArchive.Core.Models;
using DocumentArchive.Infrastructure.Data;
using DocumentArchive.Services.Validators;
using FluentValidation.TestHelper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DocumentArchive.Tests.ValidatorsTests;

public class CreateCategoryDtoValidatorTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly CreateCategoryDtoValidator _validator;

    public CreateCategoryDtoValidatorTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        // Добавляем категорию для проверки уникальности
        _context.Categories.Add(new Category { Id = Guid.NewGuid(), Name = "Existing" });
        _context.SaveChanges();

        _validator = new CreateCategoryDtoValidator(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Close();
        _connection.Dispose();
    }

    [Fact]
    public async Task Should_HaveError_When_Name_IsEmpty()
    {
        var dto = new CreateCategoryDto { Name = "" };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Should_HaveError_When_Name_ExceedsMaxLength()
    {
        var dto = new CreateCategoryDto { Name = new string('a', 101) };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Should_HaveError_When_Name_AlreadyExists()
    {
        var dto = new CreateCategoryDto { Name = "Existing" };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Should_NotHaveError_When_Name_IsUnique()
    {
        var dto = new CreateCategoryDto { Name = "NewCategory" };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Should_NotHaveError_When_Description_ExceedsMaxLength()
    {
        var dto = new CreateCategoryDto { Name = "Valid", Description = new string('a', 501) };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }
}