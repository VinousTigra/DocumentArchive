using DocumentArchive.Core.DTOs.Permission;
using FluentValidation;

namespace DocumentArchive.Services.Validators;

public class UpdatePermissionDtoValidator : AbstractValidator<UpdatePermissionDto>
{
    public UpdatePermissionDtoValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(100).When(x => x.Name != null)
            .WithMessage("Permission name must not exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(200).When(x => x.Description != null)
            .WithMessage("Description must not exceed 200 characters.");

        RuleFor(x => x.Category)
            .MaximumLength(50).When(x => x.Category != null)
            .WithMessage("Category must not exceed 50 characters.");
    }
}