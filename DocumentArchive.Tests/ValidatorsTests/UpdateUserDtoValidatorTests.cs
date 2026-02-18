using DocumentArchive.Core.DTOs.User;
using DocumentArchive.Infrastructure.Data;
using DocumentArchive.Services.Validators;
using FluentValidation.TestHelper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DocumentArchive.Tests.ValidatorsTests;

public class UpdateUserDtoValidatorTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly UpdateUserDtoValidator _validator;

    public UpdateUserDtoValidatorTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        _validator = new UpdateUserDtoValidator(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Close();
        _connection.Dispose();
    }

    [Fact]
    public async Task Should_HaveError_When_Username_ExceedsMaxLength()
    {
        var dto = new UpdateUserDto { Username = new string('a', 51) };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public async Task Should_NotHaveError_When_Username_IsNull()
    {
        var dto = new UpdateUserDto { Username = null };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public async Task Should_HaveError_When_Email_IsInvalid()
    {
        var dto = new UpdateUserDto { Email = "invalid" };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public async Task Should_NotHaveError_When_Email_IsValid()
    {
        var dto = new UpdateUserDto { Email = "valid@test.com" };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public async Task Should_NotHaveError_When_Email_IsNull()
    {
        var dto = new UpdateUserDto { Email = null };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }
}