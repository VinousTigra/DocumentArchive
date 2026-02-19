using DocumentArchive.Core.DTOs.Role;
using DocumentArchive.Infrastructure.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace DocumentArchive.Services.Validators;

public class CreateRoleDtoValidator : AbstractValidator<CreateRoleDto>
{
    private readonly AppDbContext _context;

    public CreateRoleDtoValidator(AppDbContext context)
    {
        _context = context;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Role name is required.")
            .MaximumLength(50).WithMessage("Role name must not exceed 50 characters.")
            .MustAsync(BeUniqueName).WithMessage("Role with this name already exists.");

        RuleFor(x => x.Description)
            .MaximumLength(200).When(x => x.Description != null)
            .WithMessage("Description must not exceed 200 characters.");
    }

    private async Task<bool> BeUniqueName(string name, CancellationToken cancellationToken)
    {
        return !await _context.Roles.AnyAsync(r => r.Name == name, cancellationToken);
    }
}