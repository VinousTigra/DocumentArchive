using DocumentArchive.Core.DTOs.Category;
using DocumentArchive.Core.Interfaces.Repositorys;
using FluentValidation;

namespace DocumentArchive.Services.Validators;

public class CreateCategoryDtoValidator : AbstractValidator<CreateCategoryDto>
{
    public CreateCategoryDtoValidator(ICategoryRepository categoryRepo)
    {
        var categoryRepo1 = categoryRepo;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Название категории обязательно")
            .MaximumLength(100).WithMessage("Название не должно превышать 100 символов")
            .MustAsync(async (name, cancellation) =>
            {
                var category = await categoryRepo1.FindByNameAsync(name);
                return category == null;
            })
            .WithMessage("Категория с таким названием уже существует");

        RuleFor(x => x.Description)
            .MaximumLength(500).When(x => x.Description != null)
            .WithMessage("Описание не должно превышать 500 символов");
    }
}