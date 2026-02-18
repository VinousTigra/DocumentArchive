using DocumentArchive.Core.DTOs.User;
using DocumentArchive.Infrastructure.Data;
using FluentValidation;

namespace DocumentArchive.Services.Validators;

public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
{
    public UpdateUserDtoValidator(AppDbContext context)
    {
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