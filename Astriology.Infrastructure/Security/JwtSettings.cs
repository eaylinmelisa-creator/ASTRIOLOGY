namespace Astriology.Infrastructure.Security;

/// <summary>
/// Signing and lifetime settings for issued tokens, bound from the "Jwt"
/// configuration section and validated at startup.
/// </summary>
/// <remarks>
/// <see cref="Key"/> is a secret and never appears in a tracked file. Supply it
/// through user secrets locally (<c>Jwt:Key</c>) or the <c>Jwt__Key</c> environment
/// variable in Docker.
/// <para>
/// Validation is wired explicitly in DependencyInjection rather than through
/// DataAnnotations, which would need an extra NuGet package for a handful of checks.
/// </para>
/// </remarks>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    /// <summary>Minimum key length in characters. HMAC-SHA256 needs at least 256 bits.</summary>
    public const int MinimumKeyLength = 32;

    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public string Key { get; init; } = string.Empty;

    /// <summary>
    /// Access token lifetime. Two hours is longer than the usual guidance; the
    /// trade-off is recorded in roadmap section 5.6.
    /// </summary>
    public int AccessTokenMinutes { get; init; } = 120;

    public int RefreshTokenDays { get; init; } = 15;
}
