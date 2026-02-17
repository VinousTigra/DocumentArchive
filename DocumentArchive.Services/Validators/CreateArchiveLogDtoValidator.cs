using DocumentArchive.Core.DTOs.ArchiveLog;
using DocumentArchive.Core.Interfaces.Repositorys;
using FluentValidation;

namespace DocumentArchive.Services.Validators;

public class CreateArchiveLogDtoValidator : AbstractValidator<CreateArchiveLogDto>
{
    public CreateArchiveLogDtoValidator(IDocumentRepository documentRepo, IUserRepository userRepo)
    {
        var documentRepo1 = documentRepo;
        var userRepo1 = userRepo;

        RuleFor(x => x.Action)
            .NotEmpty().WithMessage("Action is required.")
            .MaximumLength(50).WithMessage("Action must not exceed 50 characters.");

        RuleFor(x => x.ActionType)
            .IsInEnum().WithMessage("Invalid action type.");

        RuleFor(x => x.DocumentId)
            .NotEmpty().WithMessage("Document ID is required.")
            .MustAsync(async (id, cancellation) =>
            {
                var document = await documentRepo1.GetByIdAsync(id, cancellation);
                return document != null;
            })
            .WithMessage("Document with the specified ID does not exist.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.")
            .MustAsync(async (id, cancellation) =>
            {
                var user = await userRepo1.GetByIdAsync(id, cancellation);
                return user != null;
            })
            .WithMessage("User with the specified ID does not exist.");
    }
}