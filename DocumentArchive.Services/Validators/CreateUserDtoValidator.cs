using DocumentArchive.Core.DTOs.User;
using DocumentArchive.Core.Interfaces.Repositorys;
using FluentValidation;

namespace DocumentArchive.Services.Validators;

public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserDtoValidator(IUserRepository userRepo)
    {
        var userRepo1 = userRepo;

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Имя пользователя обязательно")
            .MaximumLength(50).WithMessage("Имя пользователя не должно превышать 50 символов");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email обязателен")
            .EmailAddress().WithMessage("Некорректный формат email")
            .MaximumLength(100).WithMessage("Email не должен превышать 100 символов")
            .MustAsync(async (email, _) =>
            {
                var user = await userRepo1.FindByEmailAsync(email);
                return user == null;
            })
            .WithMessage("Пользователь с таким email уже существует");
    }
}