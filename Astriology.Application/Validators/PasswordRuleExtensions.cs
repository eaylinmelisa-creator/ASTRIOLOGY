using Astriology.Domain.Constants;
using FluentValidation;

namespace Astriology.Application.Validators;

/// <summary>
/// The password policy, written once and reused by every validator that accepts a
/// password. Identity enforces the same rules; these exist so the caller gets a
/// specific message instead of a generic Identity error code.
/// </summary>
internal static class PasswordRuleExtensions
{
    public static IRuleBuilderOptions<T, string> ApplyPasswordPolicy<T>(
        this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .NotEmpty()
                .WithMessage("Şifre boş olamaz.")
            .MinimumLength(AccountRules.PasswordMinLength)
                .WithMessage($"Şifre en az {AccountRules.PasswordMinLength} karakter olmalıdır.")
            .MaximumLength(AccountRules.PasswordMaxLength)
                .WithMessage($"Şifre en fazla {AccountRules.PasswordMaxLength} karakter olabilir.")
            .Matches("[A-Z]")
                .WithMessage("Şifre en az bir büyük harf içermelidir.")
            .Matches("[a-z]")
                .WithMessage("Şifre en az bir küçük harf içermelidir.")
            .Matches("[0-9]")
                .WithMessage("Şifre en az bir rakam içermelidir.")
            .Matches("[^a-zA-Z0-9]")
                .WithMessage("Şifre en az bir özel karakter içermelidir.");
}
