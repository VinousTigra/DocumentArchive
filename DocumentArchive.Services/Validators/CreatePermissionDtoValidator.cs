using DocumentArchive.Core.DTOs.Permission;
using DocumentArchive.Infrastructure.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace DocumentArchive.Services.Validators;

public class CreatePermissionDtoValidator : AbstractValidator<CreatePermissionDto>
{
    private readonly AppDbContext _context;

    public CreatePermissionDtoValidator(AppDbContext context)
    {
        _context = context;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Permission name is required")
            .MaximumLength(100).WithMessage("Permission name must not exceed 100 characters")
            .MustAsync(BeUniqueName).WithMessage("Permission with this name already exists.");

        RuleFor(x => x.Description)
            .MaximumLength(200).When(x => x.Description != null)
            .WithMessage("Description must not exceed 200 characters");

        RuleFor(x => x.Category)
            .MaximumLength(50).When(x => x.Category != null)
            .WithMessage("Category must not exceed 50 characters");
    }

    private async Task<bool> BeUniqueName(string name, CancellationToken cancellationToken)
        => !await _context.Permissions.AnyAsync(p => p.Name == name, cancellationToken);
}
