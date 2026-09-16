using Astriology.Application.DTOs.Auth;
using Astriology.Domain.Constants;
using FluentValidation;

namespace Astriology.Application.Validators;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(request => request.UserName)
            .NotEmpty()
                .WithMessage("Rumuz boş olamaz.")
            .MinimumLength(AccountRules.UserNameMinLength)
                .WithMessage($"Rumuz en az {AccountRules.UserNameMinLength} karakter olmalıdır.")
            .MaximumLength(AccountRules.UserNameMaxLength)
                .WithMessage($"Rumuz en fazla {AccountRules.UserNameMaxLength} karakter olabilir.")
            .Must(ContainsOnlyAllowedCharacters)
                .WithMessage("Rumuz yalnızca İngilizce harf, rakam ve - . _ karakterlerini içerebilir. "
                    + "Türkçe karakter kullanılamaz.");

        RuleFor(request => request.Email)
            .NotEmpty()
                .WithMessage("E-posta boş olamaz.")
            .MaximumLength(AccountRules.EmailMaxLength)
                .WithMessage($"E-posta en fazla {AccountRules.EmailMaxLength} karakter olabilir.")
            .EmailAddress()
                .WithMessage("Geçerli bir e-posta adresi giriniz.");

        RuleFor(request => request.Password).ApplyPasswordPolicy();

        RuleFor(request => request.ConfirmPassword)
            .Equal(request => request.Password)
                .WithMessage("Şifreler eşleşmiyor.");

        RuleFor(request => request.BirthDate).ApplyBirthDatePolicy();

        RuleFor(request => request.BirthPlace)
            .MaximumLength(AccountRules.BirthPlaceMaxLength)
                .WithMessage($"Doğum yeri en fazla {AccountRules.BirthPlaceMaxLength} karakter olabilir.");
    }

    private static bool ContainsOnlyAllowedCharacters(string? userName) =>
        !string.IsNullOrEmpty(userName)
        && userName.All(character => AccountRules.AllowedUserNameCharacters.Contains(character));
}
