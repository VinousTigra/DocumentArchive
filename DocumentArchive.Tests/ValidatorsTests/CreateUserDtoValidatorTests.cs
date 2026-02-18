using DocumentArchive.Core.DTOs.User;
using DocumentArchive.Core.Models;
using DocumentArchive.Infrastructure.Data;
using DocumentArchive.Services.Validators;
using FluentValidation.TestHelper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DocumentArchive.Tests.ValidatorsTests;

public class CreateUserDtoValidatorTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly CreateUserDtoValidator _validator;

    public CreateUserDtoValidatorTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        // Добавляем пользователя для проверки уникальности
        _context.Users.Add(new User { Id = Guid.NewGuid(), Username = "existing", Email = "existing@test.com" });
        _context.SaveChanges();

        _validator = new CreateUserDtoValidator(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Close();
        _connection.Dispose();
    }

    [Fact]
    public async Task Should_HaveError_When_Username_IsEmpty()
    {
        var dto = new CreateUserDto { Username = "", Email = "test@test.com" };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public async Task Should_HaveError_When_Username_ExceedsMaxLength()
    {
        var dto = new CreateUserDto { Username = new string('a', 51), Email = "test@test.com" };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public async Task Should_HaveError_When_Email_IsEmpty()
    {
        var dto = new CreateUserDto { Username = "user", Email = "" };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Email);
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
        var dto = new CreateUserDto { Username = "new", Email = "existing@test.com" };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public async Task Should_NotHaveError_When_Email_IsUnique()
    {
        var dto = new CreateUserDto { Username = "new", Email = "new@test.com" };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }
}