using DocumentArchive.Core.DTOs.Document;
using DocumentArchive.Core.Models;
using DocumentArchive.Infrastructure.Data;
using DocumentArchive.Services.Validators;
using FluentValidation.TestHelper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DocumentArchive.Tests.ValidatorsTests;

public class UpdateDocumentDtoValidatorTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly UpdateDocumentDtoValidator _validator;

    public UpdateDocumentDtoValidatorTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        // Добавляем категорию для проверки существования
        var category = new Category { Id = Guid.NewGuid(), Name = "Test" };
        _context.Categories.Add(category);
        _context.SaveChanges();

        _validator = new UpdateDocumentDtoValidator(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Close();
        _connection.Dispose();
    }

    [Fact]
    public async Task Should_HaveError_When_Title_ExceedsMaxLength()
    {
        var dto = new UpdateDocumentDto { Title = new string('a', 201) };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public async Task Should_NotHaveError_When_Title_IsNull()
    {
        var dto = new UpdateDocumentDto { Title = null };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public async Task Should_HaveError_When_FileName_ExceedsMaxLength()
    {
        var dto = new UpdateDocumentDto { FileName = new string('a', 101) };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.FileName);
    }

    [Fact]
    public async Task Should_HaveError_When_CategoryId_DoesNotExist()
    {
        var dto = new UpdateDocumentDto { CategoryId = Guid.NewGuid() };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.CategoryId);
    }

    [Fact]
    public async Task Should_NotHaveError_When_CategoryId_Exists()
    {
        var existingCategoryId = _context.Categories.First().Id;
        var dto = new UpdateDocumentDto { CategoryId = existingCategoryId };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.CategoryId);
    }
}