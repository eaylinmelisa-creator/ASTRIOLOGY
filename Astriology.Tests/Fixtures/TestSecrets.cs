using System.Security.Cryptography;

namespace Astriology.Tests.Fixtures;

/// <summary>
/// Secrets the test run needs, generated once per process.
/// </summary>
/// <remarks>
/// Nothing here is written to a file. The fixture and the web application factory
/// both read these statics, so the host booted for HTTP tests signs tokens with the
/// same key the fixture seeded against - two independently generated keys would make
/// every token issued by one unusable by the other.
/// </remarks>
internal static class TestSecrets
{
    /// <summary>At least 32 characters, as JwtSettings requires for HMAC-SHA256.</summary>
    public static string JwtKey { get; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

    public static string AdminPassword { get; } = GeneratePassword();

    public static string DemoPassword { get; } = GeneratePassword();

    /// <summary>
    /// Satisfies the configured Identity rules: at least eight characters with an
    /// upper case letter, a lower case letter, a digit and a special character.
    /// </summary>
    private static string GeneratePassword() => $"Aa1!{Guid.NewGuid():N}";

    /// <summary>Configuration entries these secrets provide, for either host to add.</summary>
    public static Dictionary<string, string?> AsConfiguration() => new()
    {
        ["Jwt:Key"] = JwtKey,
        ["Seed:AdminPassword"] = AdminPassword,
        ["Seed:DemoPassword"] = DemoPassword,
    };
}
