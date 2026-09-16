using Astriology.Application.DTOs.Auth;
using FluentValidation;

namespace Astriology.Application.Validators;

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(request => request.CurrentPassword)
            .NotEmpty()
                .WithMessage("Mevcut şifre boş olamaz.");

        RuleFor(request => request.NewPassword).ApplyPasswordPolicy();

        RuleFor(request => request.NewPassword)
            .NotEqual(request => request.CurrentPassword)
                .WithMessage("Yeni şifre mevcut şifreyle aynı olamaz.");

        RuleFor(request => request.ConfirmNewPassword)
            .Equal(request => request.NewPassword)
                .WithMessage("Yeni şifreler eşleşmiyor.");
    }
}
