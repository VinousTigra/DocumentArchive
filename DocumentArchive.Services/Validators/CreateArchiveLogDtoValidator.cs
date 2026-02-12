using DocumentArchive.Core.DTOs.ArchiveLog;
using DocumentArchive.Infrastructure.Repositories;
using FluentValidation;

namespace DocumentArchive.Services.Validators;

public class CreateArchiveLogDtoValidator : AbstractValidator<CreateArchiveLogDto>
{
    private readonly DocumentRepository _documentRepo;
    private readonly UserRepository _userRepo;

    public CreateArchiveLogDtoValidator(DocumentRepository documentRepo, UserRepository userRepo)
    {
        _documentRepo = documentRepo;
        _userRepo = userRepo;

        RuleFor(x => x.Action)
            .NotEmpty().WithMessage("Действие обязательно")
            .MaximumLength(50).WithMessage("Действие не должно превышать 50 символов");

        RuleFor(x => x.DocumentId)
            .NotEmpty().WithMessage("ID документа обязателен")
            .MustAsync(async (id, cancellation) =>
            {
                var document = await _documentRepo.GetByIdAsync(id);
                return document != null;
            })
            .WithMessage("Документ с указанным ID не существует");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("ID пользователя обязателен")
            .MustAsync(async (id, cancellation) =>
            {
                var user = await _userRepo.GetByIdAsync(id);
                return user != null;
            })
            .WithMessage("Пользователь с указанным ID не существует");
    }
}