namespace Astriology.Infrastructure.Persistence;

/// <summary>
/// A long-lived token that lets a client obtain a new access token without
/// signing in again.
/// </summary>
/// <remarks>
/// Lives in the infrastructure layer rather than the domain: it is a detail of how
/// authentication is implemented, not part of the horoscope domain. It does not
/// derive from <c>BaseEntity</c> for the same reason.
/// <para>
/// There is no rotation. A token stays valid for its whole lifetime and is revoked
/// only at logout; see roadmap section 5.6.
/// </para>
/// </remarks>
public sealed class RefreshToken
{
    /// <summary>Required by EF Core for materialization.</summary>
    private RefreshToken()
    {
        Token = null!;
    }

    public RefreshToken(string token, int userId, DateTime expiresAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userId);

        Token = token;
        UserId = userId;
        ExpiresAt = expiresAt;
    }

    public int Id { get; private set; }

    public string Token { get; private set; }

    public int UserId { get; private init; }

    public DateTime ExpiresAt { get; private set; }

    /// <summary>Local time; see roadmap section 5.9.</summary>
    public DateTime CreatedAt { get; private set; } = DateTime.Now;

    /// <summary>Set at logout only. Null means the token has never been revoked.</summary>
    public DateTime? RevokedAt { get; private set; }

    public bool IsRevoked() => RevokedAt.HasValue;

    public bool IsExpired(DateTime now) => now >= ExpiresAt;

    /// <summary>A token is usable only while it is neither revoked nor expired.</summary>
    public bool IsActive(DateTime now) => !IsRevoked() && !IsExpired(now);

    /// <summary>
    /// Marks the token unusable. Revoking an already-revoked token keeps the original
    /// timestamp so the audit trail is not rewritten.
    /// </summary>
    public void Revoke()
    {
        RevokedAt ??= DateTime.Now;
    }
}
