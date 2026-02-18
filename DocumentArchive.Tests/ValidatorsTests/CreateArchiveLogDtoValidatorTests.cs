using DocumentArchive.Core.DTOs.ArchiveLog;
using DocumentArchive.Core.Models;
using DocumentArchive.Infrastructure.Data;
using DocumentArchive.Services.Validators;
using FluentValidation.TestHelper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DocumentArchive.Tests.ValidatorsTests;

public class CreateArchiveLogDtoValidatorTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly CreateArchiveLogDtoValidator _validator;

    public CreateArchiveLogDtoValidatorTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        // Добавляем документ и пользователя для проверки существования
        var user = new User { Id = Guid.NewGuid(), Username = "u", Email = "u@t.com" };
        var doc = new Document { Id = Guid.NewGuid(), Title = "Doc", FileName = "f.pdf" };
        _context.Users.Add(user);
        _context.Documents.Add(doc);
        _context.SaveChanges();

        _validator = new CreateArchiveLogDtoValidator(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Close();
        _connection.Dispose();
    }

    [Fact]
    public async Task Should_HaveError_When_Action_IsEmpty()
    {
        var dto = new CreateArchiveLogDto
        {
            Action = "",
            ActionType = ActionType.Created,
            DocumentId = _context.Documents.First().Id,
            UserId = _context.Users.First().Id
        };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Action);
    }

    [Fact]
    public async Task Should_HaveError_When_Action_ExceedsMaxLength()
    {
        var dto = new CreateArchiveLogDto
        {
            Action = new string('a', 51),
            ActionType = ActionType.Created,
            DocumentId = _context.Documents.First().Id,
            UserId = _context.Users.First().Id
        };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Action);
    }

    [Fact]
    public async Task Should_HaveError_When_ActionType_IsInvalid()
    {
        var dto = new CreateArchiveLogDto
        {
            Action = "Test",
            ActionType = (ActionType)999,
            DocumentId = _context.Documents.First().Id,
            UserId = _context.Users.First().Id
        };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.ActionType);
    }

    [Fact]
    public async Task Should_HaveError_When_DocumentId_DoesNotExist()
    {
        var dto = new CreateArchiveLogDto
        {
            Action = "Test",
            ActionType = ActionType.Created,
            DocumentId = Guid.NewGuid(),
            UserId = _context.Users.First().Id
        };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.DocumentId);
    }

    [Fact]
    public async Task Should_NotHaveError_When_DocumentId_Exists()
    {
        var dto = new CreateArchiveLogDto
        {
            Action = "Test",
            ActionType = ActionType.Created,
            DocumentId = _context.Documents.First().Id,
            UserId = _context.Users.First().Id
        };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.DocumentId);
    }

    [Fact]
    public async Task Should_HaveError_When_UserId_DoesNotExist()
    {
        var dto = new CreateArchiveLogDto
        {
            Action = "Test",
            ActionType = ActionType.Created,
            DocumentId = _context.Documents.First().Id,
            UserId = Guid.NewGuid()
        };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public async Task Should_NotHaveError_When_UserId_Exists()
    {
        var dto = new CreateArchiveLogDto
        {
            Action = "Test",
            ActionType = ActionType.Created,
            DocumentId = _context.Documents.First().Id,
            UserId = _context.Users.First().Id
        };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.UserId);
    }
}