namespace Astriology.Application.DTOs.Auth;

/// <summary>
/// The account as the client is allowed to see it.
/// </summary>
/// <remarks>
/// There is deliberately no password field of any kind. The hash never leaves the
/// data layer, and a test asserts that.
/// </remarks>
public sealed record UserDto(
    int Id,
    string UserName,
    string Email,
    DateOnly BirthDate,
    TimeOnly? BirthTime,
    string? BirthPlace,
    int? AscendantSignId,
    int? SunSignId,
    bool IsActive,
    DateTime CreatedAt,
    IReadOnlyList<string> Roles);
