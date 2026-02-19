using DocumentArchive.Core.DTOs.User;
using DocumentArchive.Infrastructure.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace DocumentArchive.Services.Validators;

public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
{
    private readonly AppDbContext _context;

    public CreateUserDtoValidator(AppDbContext context)
    {
        _context = context;


        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email обязателен")
            .EmailAddress().WithMessage("Некорректный формат email")
            .MaximumLength(100).WithMessage("Email не должен превышать 100 символов")
            .MustAsync(BeUniqueEmail).WithMessage("Пользователь с таким email уже существует");

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Имя пользователя обязательно")
            .MaximumLength(50).WithMessage("Имя пользователя не должно превышать 50 символов")
            .MustAsync(BeUniqueUsername).WithMessage("Пользователь с таким именем уже существует");
    }

    private async Task<bool> BeUniqueEmail(string email, CancellationToken cancellationToken)
    {
        return !await _context.Users.AnyAsync(u => u.Email == email, cancellationToken);
    }

    private async Task<bool> BeUniqueUsername(string username, CancellationToken cancellationToken)
    {
        return !await _context.Users.AnyAsync(u => u.Username == username, cancellationToken);
    }
}