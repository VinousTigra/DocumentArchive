using DocumentArchive.Core.DTOs.Auth;
using FluentValidation;

namespace DocumentArchive.Services.Validators;

public class ConfirmEmailDtoValidator : AbstractValidator<ConfirmEmailDto>
{
    public ConfirmEmailDtoValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token is required");
    }
}