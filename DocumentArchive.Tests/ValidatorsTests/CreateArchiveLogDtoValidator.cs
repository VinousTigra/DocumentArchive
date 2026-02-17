using DocumentArchive.Core.DTOs.ArchiveLog;
using DocumentArchive.Core.Interfaces;
using DocumentArchive.Core.Interfaces.Repositorys;
using FluentValidation;

namespace DocumentArchive.Services.Validators;

public class CreateArchiveLogDtoValidator : AbstractValidator<CreateArchiveLogDto>
{
    private readonly IDocumentRepository _documentRepo;
    private readonly IUserRepository _userRepo;

    public CreateArchiveLogDtoValidator(
        IDocumentRepository documentRepo,
        IUserRepository userRepo)
    {
        _documentRepo = documentRepo;
        _userRepo = userRepo;

        RuleFor(x => x.Action)
            .NotEmpty().WithMessage("Действие обязательно")
            .MaximumLength(50).WithMessage("Действие не должно превышать 50 символов");

        RuleFor(x => x.ActionType)
            .IsInEnum().WithMessage("Некорректный тип действия");

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