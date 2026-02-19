using DocumentArchive.Core.DTOs.DocumentVersion;
using FluentValidation;

namespace DocumentArchive.Services.Validators;

public class UpdateDocumentVersionDtoValidator : AbstractValidator<UpdateDocumentVersionDto>
{
    public UpdateDocumentVersionDtoValidator()
    {
        RuleFor(x => x.Comment)
            .MaximumLength(500).When(x => x.Comment != null)
            .WithMessage("Comment must not exceed 500 characters.");
    }
}