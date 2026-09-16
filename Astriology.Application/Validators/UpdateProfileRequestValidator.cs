using Astriology.Application.DTOs.Users;
using Astriology.Domain.Constants;
using FluentValidation;

namespace Astriology.Application.Validators;

public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(request => request.BirthDate).ApplyBirthDatePolicy();

        RuleFor(request => request.BirthPlace)
            .MaximumLength(AccountRules.BirthPlaceMaxLength)
                .WithMessage($"Doğum yeri en fazla {AccountRules.BirthPlaceMaxLength} karakter olabilir.");
    }
}
