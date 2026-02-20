using DocumentArchive.Core.DTOs.Document;
using DocumentArchive.Infrastructure.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace DocumentArchive.Services.Validators;

public class CreateDocumentDtoValidator : AbstractValidator<CreateDocumentDto>
{
    private readonly AppDbContext _context;

    public CreateDocumentDtoValidator(AppDbContext context)
    {
        _context = context;

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Название документа обязательно")
            .MaximumLength(200).WithMessage("Название не должно превышать 200 символов");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("Имя файла обязательно")
            .MaximumLength(100).WithMessage("Имя файла не должно превышать 100 символов");

        RuleFor(x => x.CategoryId)
            .MustAsync(BeExistingCategory)
            .WithMessage("Категория с указанным ID не существует")
            .When(x => x.CategoryId.HasValue); // проверяем только если ID указан

        RuleFor(x => x.UserId)
            .MustAsync(BeExistingUser)
            .WithMessage("Пользователь с указанным ID не существует")
            .When(x => x.UserId.HasValue);
    }

    private async Task<bool> BeExistingCategory(Guid? categoryId, CancellationToken cancellationToken)
    {
        if (!categoryId.HasValue) return true;
        return await _context.Categories.AnyAsync(c => c.Id == categoryId.Value, cancellationToken);
    }

    private async Task<bool> BeExistingUser(Guid? userId, CancellationToken cancellationToken)
    {
        if (!userId.HasValue) return true;
        return await _context.Users.AnyAsync(u => u.Id == userId.Value, cancellationToken);
    }
}