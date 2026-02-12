using DocumentArchive.Core.DTOs.User;
using DocumentArchive.Core.Interfaces;
using FluentValidation;

namespace DocumentArchive.Services.Validators;

public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
{
    private readonly IUserRepository _userRepo;

    public CreateUserDtoValidator(IUserRepository userRepo)
    {
        _userRepo = userRepo;

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Имя пользователя обязательно")
            .MaximumLength(50).WithMessage("Имя пользователя не должно превышать 50 символов");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email обязателен")
            .EmailAddress().WithMessage("Некорректный формат email")
            .MaximumLength(100).WithMessage("Email не должен превышать 100 символов")
            .MustAsync(async (email, cancellation) =>
            {
                var user = await _userRepo.FindByEmailAsync(email);
                return user == null;
            })
            .WithMessage("Пользователь с таким email уже существует");
    }
}