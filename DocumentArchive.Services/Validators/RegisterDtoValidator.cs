using DocumentArchive.Core.DTOs.Auth;
using DocumentArchive.Infrastructure.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace DocumentArchive.Services.Validators;

public class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
    private readonly AppDbContext _context;

    public RegisterDtoValidator(AppDbContext context)
    {
        _context = context;

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(200).WithMessage("Email must not exceed 200 characters")
            .MustAsync(BeUniqueEmail).WithMessage("Email already registered");

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required")
            .Length(3, 50).WithMessage("Username must be between 3 and 50 characters")
            .Matches("^[a-zA-Z0-9_]+$").WithMessage("Username can only contain letters, numbers, and underscores")
            .MustAsync(BeUniqueUsername).WithMessage("Username already taken");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Passwords do not match");

        When(x => x.DateOfBirth.HasValue, () =>
        {
            RuleFor(x => x.DateOfBirth.Value)
                .LessThan(DateTime.Today.AddYears(-18))
                .WithMessage("You must be at least 18 years old")
                .LessThan(DateTime.Today)
                .WithMessage("Date of birth cannot be in the future");
        });

        When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber), () =>
        {
            RuleFor(x => x.PhoneNumber)
                .Matches(@"^\+?[0-9]{10,15}$").WithMessage("Invalid phone number format");
        });
    }

    private async Task<bool> BeUniqueEmail(string email, CancellationToken cancellationToken)
        => !await _context.Users.AnyAsync(u => u.Email == email, cancellationToken);

    private async Task<bool> BeUniqueUsername(string username, CancellationToken cancellationToken)
        => !await _context.Users.AnyAsync(u => u.Username == username, cancellationToken);
}