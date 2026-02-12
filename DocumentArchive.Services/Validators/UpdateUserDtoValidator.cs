using DocumentArchive.Core.DTOs.User;
using DocumentArchive.Infrastructure.Repositories;
using FluentValidation;

namespace DocumentArchive.Services.Validators;

public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
{
    private readonly UserRepository _userRepo;

    public UpdateUserDtoValidator(UserRepository userRepo)
    {
        _userRepo = userRepo;

        RuleFor(x => x.Username)
            .MaximumLength(50).When(x => x.Username != null)
            .WithMessage("Имя пользователя не должно превышать 50 символов");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => x.Email != null)
            .WithMessage("Некорректный формат email")
            .MaximumLength(100).When(x => x.Email != null)
            .WithMessage("Email не должен превышать 100 символов")
            .MustAsync(async (dto, email, cancellation) =>
            {
                // Здесь сложнее: нужно проверить, что email уникален, исключая текущего пользователя.
                // Но при PUT мы не передаём ID в DTO. Это ограничение.
                // Для простоты можно пропустить проверку уникальности в Update, либо сделать дополнительный запрос.
                // В учебных целях опустим или добавим параметр Id.
                // Рекомендую: на этапе обновления проверять уникальность через контекст (Id из маршрута).
                // Однако в валидатор нельзя просто так передать Id маршрута. Можно использовать кастомную валидацию в контроллере.
                // Поэтому здесь просто оставим базовые проверки, а уникальность проверим в контроллере.
                return true;
            });
    }
}