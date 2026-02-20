using DocumentArchive.Core.DTOs.Category;
using DocumentArchive.Infrastructure.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace DocumentArchive.Services.Validators;

public class CreateCategoryDtoValidator : AbstractValidator<CreateCategoryDto>
{
    private readonly AppDbContext _context;

    public CreateCategoryDtoValidator(AppDbContext context)
    {
        _context = context;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Название категории обязательно")
            .MaximumLength(100).WithMessage("Название не должно превышать 100 символов")
            .MustAsync(BeUniqueName).WithMessage("Категория с таким названием уже существует");

        RuleFor(x => x.Description)
            .MaximumLength(500).When(x => x.Description != null)
            .WithMessage("Описание не должно превышать 500 символов");
    }

    private async Task<bool> BeUniqueName(string name, CancellationToken cancellationToken)
    {
        return !await _context.Categories.AnyAsync(c => c.Name == name, cancellationToken);
    }
}