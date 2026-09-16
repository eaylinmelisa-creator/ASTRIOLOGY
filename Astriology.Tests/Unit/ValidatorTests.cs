using Astriology.Application.DTOs.Auth;
using Astriology.Application.Validators;

namespace Astriology.Tests.Unit;

/// <summary>
/// Pure validation rules, no database and no container.
/// </summary>
public sealed class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    private static RegisterRequest Valid() => new(
        UserName: "gecerli_kullanici",
        Email: "kullanici@astriology.local",
        Password: "Gecerli1!Sifre",
        ConfirmPassword: "Gecerli1!Sifre",
        BirthDate: new DateOnly(1995, 5, 20),
        BirthTime: new TimeOnly(14, 30),
        BirthPlace: "İzmir");

    [Fact]
    public void Accepts_a_well_formed_request()
    {
        var result = _validator.Validate(Valid());

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("çağrı")]      // Turkish ç and ğ
    [InlineData("şeyma")]      // Turkish ş
    [InlineData("İbrahim")]    // Turkish dotted capital I
    [InlineData("user name")]  // space
    [InlineData("user@name")]  // symbol outside the allow list
    public void Rejects_user_names_outside_the_allowed_character_set(string userName)
    {
        var result = _validator.Validate(Valid() with { UserName = userName });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RegisterRequest.UserName));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("gecersiz")]
    [InlineData("gecersiz@")]
    [InlineData("@astriology.local")]
    public void Rejects_missing_or_malformed_email(string email)
    {
        var result = _validator.Validate(Valid() with { Email = email });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RegisterRequest.Email));
    }

    [Theory]
    [InlineData("Ab1!ab", "shorter than the minimum length")]
    [InlineData("gecerli1!sifre", "no upper case letter")]
    [InlineData("GECERLI1!SIFRE", "no lower case letter")]
    [InlineData("GecerliSifre!", "no digit")]
    [InlineData("GecerliSifre1", "no special character")]
    public void Rejects_passwords_that_break_the_policy(string password, string reason)
    {
        var result = _validator.Validate(Valid() with { Password = password, ConfirmPassword = password });

        Assert.False(result.IsValid, $"Expected rejection because the password has {reason}.");
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RegisterRequest.Password));
    }

    [Fact]
    public void Rejects_a_confirmation_that_does_not_match()
    {
        var result = _validator.Validate(Valid() with { ConfirmPassword = "Baska1!Sifre" });

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(RegisterRequest.ConfirmPassword));
    }

    [Fact]
    public void Rejects_a_birth_date_in_the_future()
    {
        var tomorrow = DateOnly.FromDateTime(DateTime.Now).AddDays(1);

        var result = _validator.Validate(Valid() with { BirthDate = tomorrow });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RegisterRequest.BirthDate));
    }

    [Fact]
    public void Rejects_an_implausibly_old_birth_date()
    {
        var result = _validator.Validate(Valid() with { BirthDate = new DateOnly(1850, 1, 1) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RegisterRequest.BirthDate));
    }
}

public sealed class ChangePasswordRequestValidatorTests
{
    private readonly ChangePasswordRequestValidator _validator = new();

    [Fact]
    public void Accepts_a_well_formed_request()
    {
        var result = _validator.Validate(new ChangePasswordRequest("Eski1!Sifre", "Yeni1!Sifre", "Yeni1!Sifre"));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("Ab1!ab")]
    [InlineData("yeni1!sifre")]
    [InlineData("YENI1!SIFRE")]
    [InlineData("YeniSifre!")]
    [InlineData("YeniSifre1")]
    public void Rejects_a_new_password_that_breaks_the_policy(string newPassword)
    {
        var result = _validator.Validate(
            new ChangePasswordRequest("Eski1!Sifre", newPassword, newPassword));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Rejects_reusing_the_current_password()
    {
        var result = _validator.Validate(new ChangePasswordRequest("Ayni1!Sifre", "Ayni1!Sifre", "Ayni1!Sifre"));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Rejects_a_confirmation_that_does_not_match()
    {
        var result = _validator.Validate(new ChangePasswordRequest("Eski1!Sifre", "Yeni1!Sifre", "Farkli1!Sifre"));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Rejects_an_empty_current_password()
    {
        var result = _validator.Validate(new ChangePasswordRequest("", "Yeni1!Sifre", "Yeni1!Sifre"));

        Assert.False(result.IsValid);
    }
}
