using DocumentArchive.Core.DTOs.Document;
using DocumentArchive.Infrastructure.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace DocumentArchive.Services.Validators;

public class UpdateDocumentDtoValidator : AbstractValidator<UpdateDocumentDto>
{
    private readonly AppDbContext _context;

    public UpdateDocumentDtoValidator(AppDbContext context)
    {
        _context = context;

        RuleFor(x => x.Title)
            .MaximumLength(200).When(x => x.Title != null)
            .WithMessage("Название не должно превышать 200 символов");

        RuleFor(x => x.FileName)
            .MaximumLength(100).When(x => x.FileName != null)
            .WithMessage("Имя файла не должно превышать 100 символов");

        RuleFor(x => x.CategoryId)
            .MustAsync(BeExistingCategory)
            .WithMessage("Категория с указанным ID не существует")
            .When(x => x.CategoryId.HasValue);
    }

    private async Task<bool> BeExistingCategory(Guid? categoryId, CancellationToken cancellationToken)
    {
        if (!categoryId.HasValue) return true;
        return await _context.Categories.AnyAsync(c => c.Id == categoryId.Value, cancellationToken);
    }
}