namespace Astriology.Domain.Constants;

/// <summary>
/// Account rules shared by the Identity configuration and the request validators.
/// </summary>
/// <remarks>
/// One definition on purpose. Identity enforces these when it creates a user and
/// FluentValidation enforces them at the boundary so the caller gets a clear message
/// instead of a raw Identity error code; if the two drifted apart, one of them would
/// silently stop matching reality.
/// <para>
/// The frontend zod schema mirrors the same numbers and must be changed together
/// with this file (roadmap section 4.1).
/// </para>
/// </remarks>
public static class AccountRules
{
    /// <summary>
    /// Characters a user name may contain. Turkish letters are excluded: they break
    /// URLs and comparisons under the .NET Turkish-I behaviour.
    /// </summary>
    public const string AllowedUserNameCharacters =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._";

    public const int UserNameMinLength = 3;

    public const int UserNameMaxLength = 32;

    public const int PasswordMinLength = 8;

    /// <summary>Guards against denial of service through the hashing work factor.</summary>
    public const int PasswordMaxLength = 128;

    public const int EmailMaxLength = 256;

    public const int BirthPlaceMaxLength = 100;

    /// <summary>
    /// Nobody older than this is a plausible sign-up, and it stops obvious typos such
    /// as a year of 1090 from reaching the database.
    /// </summary>
    public const int MaxAgeInYears = 120;
}
