using DocumentArchive.Core.DTOs.DocumentVersion;
using DocumentArchive.Infrastructure.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace DocumentArchive.Services.Validators;

public class CreateDocumentVersionDtoValidator : AbstractValidator<CreateDocumentVersionDto>
{
    private readonly AppDbContext _context;

    public CreateDocumentVersionDtoValidator(AppDbContext context)
    {
        _context = context;

        RuleFor(x => x.DocumentId)
            .NotEmpty().WithMessage("Document ID is required")
            .MustAsync(BeExistingDocument).WithMessage("Document with this ID does not exist.");

        RuleFor(x => x.VersionNumber)
            .GreaterThan(0).WithMessage("Version number must be positive")
            .MustAsync(BeUniqueVersionNumber).WithMessage("Version number already exists for this document.");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("File name is required")
            .MaximumLength(255).WithMessage("File name must not exceed 255 characters");

        RuleFor(x => x.FileSize)
            .GreaterThan(0).WithMessage("File size must be greater than 0");

        RuleFor(x => x.Comment)
            .MaximumLength(500).When(x => x.Comment != null)
            .WithMessage("Comment must not exceed 500 characters");
    }

    private async Task<bool> BeExistingDocument(Guid documentId, CancellationToken cancellationToken)
        => await _context.Documents.AnyAsync(d => d.Id == documentId, cancellationToken);

    private async Task<bool> BeUniqueVersionNumber(CreateDocumentVersionDto dto, int versionNumber, CancellationToken cancellationToken)
        => !await _context.DocumentVersions
            .AnyAsync(v => v.DocumentId == dto.DocumentId && v.VersionNumber == versionNumber, cancellationToken);
}