using DocumentArchive.Core.DTOs.Category;
using DocumentArchive.Infrastructure.Data;
using FluentValidation;

namespace DocumentArchive.Services.Validators;

public class UpdateCategoryDtoValidator : AbstractValidator<UpdateCategoryDto>
{
    public UpdateCategoryDtoValidator(AppDbContext context)
    {
        // Для обновления проверяем только длину, так как имя может не меняться
        RuleFor(x => x.Name)
            .MaximumLength(100).When(x => x.Name != null)
            .WithMessage("Название не должно превышать 100 символов");

        RuleFor(x => x.Description)
            .MaximumLength(500).When(x => x.Description != null)
            .WithMessage("Описание не должно превышать 500 символов");
    }
}