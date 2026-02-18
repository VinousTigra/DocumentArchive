using DocumentArchive.Core.DTOs.User;
using DocumentArchive.Core.Interfaces.Repositorys;
using FluentValidation;

namespace DocumentArchive.Services.Validators;

public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
{
    private readonly IUserRepository _userRepo;

    public UpdateUserDtoValidator(IUserRepository userRepo)
    {
        _userRepo = userRepo;

        RuleFor(x => x.Username)
            .MaximumLength(50).When(x => x.Username != null)
            .WithMessage("Имя пользователя не должно превышать 50 символов");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => x.Email != null)
            .WithMessage("Некорректный формат email")
            .MaximumLength(100).When(x => x.Email != null)
            .WithMessage("Email не должен превышать 100 символов");
    }
}