using DocumentArchive.Core.DTOs.ArchiveLog;
using DocumentArchive.Infrastructure.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace DocumentArchive.Services.Validators;

public class CreateArchiveLogDtoValidator : AbstractValidator<CreateArchiveLogDto>
{
    private readonly AppDbContext _context;

    public CreateArchiveLogDtoValidator(AppDbContext context)
    {
        _context = context;

        RuleFor(x => x.Action)
            .NotEmpty().WithMessage("Action is required.")
            .MaximumLength(50).WithMessage("Action must not exceed 50 characters.");

        RuleFor(x => x.ActionType)
            .IsInEnum().WithMessage("Invalid action type.");

        RuleFor(x => x.DocumentId)
            .NotEmpty().WithMessage("Document ID is required.")
            .MustAsync(BeExistingDocument)
            .WithMessage("Document with the specified ID does not exist.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.")
            .MustAsync(BeExistingUser)
            .WithMessage("User with the specified ID does not exist.");
    }

    private async Task<bool> BeExistingDocument(Guid documentId, CancellationToken cancellationToken)
    {
        return await _context.Documents.AnyAsync(d => d.Id == documentId, cancellationToken);
    }

    private async Task<bool> BeExistingUser(Guid userId, CancellationToken cancellationToken)
    {
        return await _context.Users.AnyAsync(u => u.Id == userId, cancellationToken);
    }
}