namespace Astriology.Infrastructure.Security;

/// <summary>
/// Claim names this application issues and reads.
/// </summary>
/// <remarks>
/// Short names are used deliberately and inbound claim mapping is switched off in the
/// JWT bearer options, so what the token carries is exactly what the server reads.
/// With the default mapping on, "sub" silently becomes a long WS-Federation URI and
/// lookups by "sub" return nothing.
/// </remarks>
public static class TokenClaimNames
{
    /// <summary>Subject: the user's id.</summary>
    public const string Subject = "sub";

    public const string UserName = "unique_name";

    public const string Email = "email";

    /// <summary>Token id, unique per issued access token.</summary>
    public const string TokenId = "jti";

    public const string Role = "role";
}
