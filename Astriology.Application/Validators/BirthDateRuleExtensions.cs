using Astriology.Domain.Constants;
using FluentValidation;

namespace Astriology.Application.Validators;

/// <summary>
/// Birth date bounds, shared by registration and profile updates.
/// </summary>
internal static class BirthDateRuleExtensions
{
    public static IRuleBuilderOptions<T, DateOnly> ApplyBirthDatePolicy<T>(
        this IRuleBuilder<T, DateOnly> ruleBuilder)
    {
        // Local time, per the project's time policy (roadmap section 5.9).
        var today = DateOnly.FromDateTime(DateTime.Now);
        var earliest = today.AddYears(-AccountRules.MaxAgeInYears);

        return ruleBuilder
            .NotEqual(default(DateOnly))
                .WithMessage("Doğum tarihi zorunludur.")
            .LessThanOrEqualTo(today)
                .WithMessage("Doğum tarihi gelecekte olamaz.")
            .GreaterThanOrEqualTo(earliest)
                .WithMessage($"Doğum tarihi {AccountRules.MaxAgeInYears} yıldan eski olamaz.");
    }
}
