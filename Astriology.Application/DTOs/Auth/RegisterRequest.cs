namespace Astriology.Application.DTOs.Auth;

/// <summary>
/// Sign-up payload. Everyone who registers receives the User role; there is no way
/// to request a different one.
/// </summary>
/// <param name="UserName">ASCII only - Turkish characters are rejected.</param>
/// <param name="BirthTime">Optional. Without it the ascendant cannot be calculated.</param>
public sealed record RegisterRequest(
    string UserName,
    string Email,
    string Password,
    string ConfirmPassword,
    DateOnly BirthDate,
    TimeOnly? BirthTime,
    string? BirthPlace);
