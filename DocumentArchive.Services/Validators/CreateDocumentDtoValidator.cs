using DocumentArchive.Core.DTOs.Document;
using DocumentArchive.Core.Interfaces.Repositorys;
using FluentValidation;

namespace DocumentArchive.Services.Validators;

public class CreateDocumentDtoValidator : AbstractValidator<CreateDocumentDto>
{
    public CreateDocumentDtoValidator(
        ICategoryRepository categoryRepo,    
        IUserRepository userRepo)           
    {
        var categoryRepo1 = categoryRepo;
        var userRepo1 = userRepo;

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Название документа обязательно")
            .MaximumLength(200).WithMessage("Название не должно превышать 200 символов");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("Имя файла обязательно")
            .MaximumLength(100).WithMessage("Имя файла не должно превышать 100 символов");

        RuleFor(x => x.CategoryId)
            .MustAsync(async (id, _) =>
            {
                if (!id.HasValue) return true;
                var category = await categoryRepo1.GetByIdAsync(id.Value);
                return category != null;
            })
            .WithMessage("Категория с указанным ID не существует");

        RuleFor(x => x.UserId)
            .MustAsync(async (id, _) =>
            {
                if (!id.HasValue) return true;
                var user = await userRepo1.GetByIdAsync(id.Value);
                return user != null;
            })
            .WithMessage("Пользователь с указанным ID не существует");
    }
}