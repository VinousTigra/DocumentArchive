using DocumentArchive.Core.DTOs.Document;
using DocumentArchive.Core.Interfaces;
using DocumentArchive.Core.Interfaces.Repositorys;
using FluentValidation;

namespace DocumentArchive.Services.Validators;

public class UpdateDocumentDtoValidator : AbstractValidator<UpdateDocumentDto>
{
    private readonly ICategoryRepository _categoryRepo;

    public UpdateDocumentDtoValidator(ICategoryRepository categoryRepo)
    {
        _categoryRepo = categoryRepo;

        RuleFor(x => x.Title)
            .MaximumLength(200).When(x => x.Title != null)
            .WithMessage("Название не должно превышать 200 символов");

        RuleFor(x => x.FileName)
            .MaximumLength(100).When(x => x.FileName != null)
            .WithMessage("Имя файла не должно превышать 100 символов");

        RuleFor(x => x.CategoryId)
            .MustAsync(async (id, cancellation) =>
            {
                if (!id.HasValue) return true;
                var category = await _categoryRepo.GetByIdAsync(id.Value);
                return category != null;
            })
            .WithMessage("Категория с указанным ID не существует");
    }
}