using Astriology.Application.DTOs.Auth;

namespace Astriology.Tests.Fixtures;

/// <summary>
/// Builds registration payloads with unique identifiers.
/// </summary>
/// <remarks>
/// The test database is created once per run and every test shares it, so a test
/// that needs its own account must not collide with another. Uniqueness comes from a
/// GUID rather than a counter so ordering never matters.
/// </remarks>
internal static class TestAccounts
{
    /// <summary>A password satisfying the configured Identity policy.</summary>
    public const string ValidPassword = "Gecerli1!Sifre";

    public static RegisterRequest NewRegistration(
        string? userName = null,
        string? email = null,
        string password = ValidPassword)
    {
        var suffix = Guid.NewGuid().ToString("N")[..12];

        return new RegisterRequest(
            UserName: userName ?? $"user_{suffix}",
            Email: email ?? $"user_{suffix}@astriology.local",
            Password: password,
            ConfirmPassword: password,
            BirthDate: new DateOnly(1995, 5, 20),
            BirthTime: new TimeOnly(14, 30),
            BirthPlace: "İzmir");
    }
}
