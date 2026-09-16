namespace Astriology.Application.DTOs.Users;

/// <summary>
/// Fields a signed-in user may change about themselves.
/// </summary>
/// <remarks>
/// User name, email and roles are absent on purpose: they are identity, not profile,
/// and letting them travel in a profile update would be an over-posting hole.
/// </remarks>
public sealed record UpdateProfileRequest(
    DateOnly BirthDate,
    TimeOnly? BirthTime,
    string? BirthPlace);
