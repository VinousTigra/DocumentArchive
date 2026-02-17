using DocumentArchive.Core.DTOs.Category;
using DocumentArchive.Core.Interfaces;
using DocumentArchive.Core.Interfaces.Repositorys;
using FluentValidation;

namespace DocumentArchive.Services.Validators;

public class UpdateCategoryDtoValidator : AbstractValidator<UpdateCategoryDto>
{
    private readonly ICategoryRepository _categoryRepo;

    public UpdateCategoryDtoValidator(ICategoryRepository categoryRepo)
    {
        _categoryRepo = categoryRepo;

        RuleFor(x => x.Name)
            .MaximumLength(100).When(x => x.Name != null)
            .WithMessage("Название не должно превышать 100 символов");

        RuleFor(x => x.Description)
            .MaximumLength(500).When(x => x.Description != null)
            .WithMessage("Описание не должно превышать 500 символов");
    }
}