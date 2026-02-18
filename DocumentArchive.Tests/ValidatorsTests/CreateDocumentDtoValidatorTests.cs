using DocumentArchive.Core.DTOs.Document;
using DocumentArchive.Core.Models;
using DocumentArchive.Infrastructure.Data;
using DocumentArchive.Services.Validators;
using FluentValidation.TestHelper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DocumentArchive.Tests.ValidatorsTests;

public class CreateDocumentDtoValidatorTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly CreateDocumentDtoValidator _validator;

    public CreateDocumentDtoValidatorTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        // Заполняем тестовыми данными
        var category = new Category { Id = Guid.NewGuid(), Name = "Test Category" };
        var user = new User { Id = Guid.NewGuid(), Username = "testuser", Email = "test@test.com" };
        _context.Categories.Add(category);
        _context.Users.Add(user);
        _context.SaveChanges();

        _validator = new CreateDocumentDtoValidator(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Close();
        _connection.Dispose();
    }

    [Fact]
    public async Task Should_HaveError_When_Title_IsEmpty()
    {
        var dto = new CreateDocumentDto { Title = "", FileName = "test.pdf" };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public async Task Should_HaveError_When_Title_ExceedsMaxLength()
    {
        var dto = new CreateDocumentDto { Title = new string('a', 201), FileName = "test.pdf" };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public async Task Should_HaveError_When_FileName_IsEmpty()
    {
        var dto = new CreateDocumentDto { Title = "Title", FileName = "" };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.FileName);
    }

    [Fact]
    public async Task Should_HaveError_When_FileName_ExceedsMaxLength()
    {
        var dto = new CreateDocumentDto { Title = "Title", FileName = new string('a', 101) };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.FileName);
    }

    [Fact]
    public async Task Should_HaveError_When_CategoryId_DoesNotExist()
    {
        var dto = new CreateDocumentDto
        {
            Title = "Title",
            FileName = "test.pdf",
            CategoryId = Guid.NewGuid()
        };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.CategoryId);
    }

    [Fact]
    public async Task Should_NotHaveError_When_CategoryId_Exists()
    {
        var existingCategoryId = _context.Categories.First().Id;
        var dto = new CreateDocumentDto
        {
            Title = "Title",
            FileName = "test.pdf",
            CategoryId = existingCategoryId
        };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.CategoryId);
    }

    [Fact]
    public async Task Should_HaveError_When_UserId_DoesNotExist()
    {
        var dto = new CreateDocumentDto
        {
            Title = "Title",
            FileName = "test.pdf",
            UserId = Guid.NewGuid()
        };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public async Task Should_NotHaveError_When_UserId_Exists()
    {
        var existingUserId = _context.Users.First().Id;
        var dto = new CreateDocumentDto
        {
            Title = "Title",
            FileName = "test.pdf",
            UserId = existingUserId
        };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.UserId);
    }
}