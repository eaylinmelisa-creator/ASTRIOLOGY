namespace Astriology.Application.DTOs.Auth;

/// <summary>
/// What a successful register, login or refresh returns.
/// </summary>
/// <param name="AccessToken">Short-lived bearer token.</param>
/// <param name="RefreshToken">
/// Long-lived token. There is no rotation: refreshing returns the same value, so a
/// client can keep the one it already stored (roadmap section 5.6).
/// </param>
public sealed record AuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    UserDto User);
