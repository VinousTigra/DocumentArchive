using DocumentArchive.Core.DTOs.Role;
using FluentValidation;

namespace DocumentArchive.Services.Validators;

public class UpdateRoleDtoValidator : AbstractValidator<UpdateRoleDto>
{
    public UpdateRoleDtoValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(50).When(x => x.Name != null)
            .WithMessage("Role name must not exceed 50 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(200).When(x => x.Description != null)
            .WithMessage("Description must not exceed 200 characters.");
    }
}