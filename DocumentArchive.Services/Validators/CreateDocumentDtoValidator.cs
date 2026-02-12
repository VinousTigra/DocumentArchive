using DocumentArchive.Core.DTOs.Document;
using DocumentArchive.Infrastructure.Repositories;
using FluentValidation;

namespace DocumentArchive.Services.Validators;

public class CreateDocumentDtoValidator : AbstractValidator<CreateDocumentDto>
{
    private readonly CategoryRepository _categoryRepo;
    private readonly UserRepository _userRepo;

    public CreateDocumentDtoValidator(CategoryRepository categoryRepo, UserRepository userRepo)
    {
        _categoryRepo = categoryRepo;
        _userRepo = userRepo;

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Название документа обязательно")
            .MaximumLength(200).WithMessage("Название не должно превышать 200 символов");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("Имя файла обязательно")
            .MaximumLength(100).WithMessage("Имя файла не должно превышать 100 символов");

        RuleFor(x => x.CategoryId)
            .MustAsync(async (id, cancellation) =>
            {
                if (!id.HasValue) return true;
                var category = await _categoryRepo.GetByIdAsync(id.Value);
                return category != null;
            })
            .WithMessage("Категория с указанным ID не существует");

        RuleFor(x => x.UserId)
            .MustAsync(async (id, cancellation) =>
            {
                if (!id.HasValue) return true;
                var user = await _userRepo.GetByIdAsync(id.Value);
                return user != null;
            })
            .WithMessage("Пользователь с указанным ID не существует");
    }
}