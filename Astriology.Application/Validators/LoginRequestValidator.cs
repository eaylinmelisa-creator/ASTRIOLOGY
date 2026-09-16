using Astriology.Application.DTOs.Auth;
using FluentValidation;

namespace Astriology.Application.Validators;

/// <summary>
/// Presence only. The password policy is deliberately not applied here: rejecting a
/// short password at sign-in would tell an attacker their guess failed the format
/// check rather than the credential check, and it would lock out accounts created
/// before a policy change.
/// </summary>
public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.UserName)
            .NotEmpty()
                .WithMessage("Rumuz boş olamaz.");

        RuleFor(request => request.Password)
            .NotEmpty()
                .WithMessage("Şifre boş olamaz.");
    }
}
