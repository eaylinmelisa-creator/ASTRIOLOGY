namespace Astriology.Application.Interfaces;

/// <param name="Value">Encoded token string.</param>
public sealed record IssuedToken(string Value, DateTime ExpiresAt);

/// <summary>A refresh token that was found, is unexpired and has not been revoked.</summary>
public sealed record ActiveRefreshToken(int UserId, DateTime ExpiresAt);

/// <summary>
/// Issues and manages authentication tokens.
/// </summary>
/// <remarks>
/// The signature takes primitives rather than a user object: the implementation
/// lives in the infrastructure layer next to Identity, but the contract must stay
/// free of Identity types so this layer keeps its zero-framework dependency rule.
/// </remarks>
public interface ITokenService
{
    /// <summary>Creates a signed access token carrying the id, name and roles as claims.</summary>
    IssuedToken CreateAccessToken(int userId, string userName, string email, IEnumerable<string> roles);

    /// <summary>Creates and stores a refresh token for the user.</summary>
    Task<IssuedToken> CreateRefreshTokenAsync(int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the token's owner and expiry when it exists, is unexpired and has not
    /// been revoked; null otherwise. Callers must not distinguish the failure reasons
    /// to the client - doing so tells an attacker which tokens are real.
    /// </summary>
    Task<ActiveRefreshToken?> ResolveActiveRefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken);

    /// <summary>
    /// Marks the token unusable. Returns false when no such token exists.
    /// Revoking is the only thing that ever fills RevokedAt (roadmap section 5.6).
    /// </summary>
    Task<bool> RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);
}
