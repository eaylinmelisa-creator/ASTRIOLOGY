using Astriology.Application.DTOs.Auth;

namespace Astriology.Infrastructure.Identity;

/// <summary>
/// Hand-written mapping from the Identity user to the DTO the API returns.
/// </summary>
/// <remarks>
/// Lives here rather than in Astriology.Application/Mappings because
/// <see cref="ApplicationUser"/> is an infrastructure type; the application layer
/// must not see it.
/// </remarks>
internal static class ApplicationUserMappings
{
    /// <summary>
    /// Copies only the fields a client may see. PasswordHash, security stamp and
    /// lockout state have no counterpart in <see cref="UserDto"/> and cannot leak
    /// through this method.
    /// </summary>
    public static UserDto ToDto(this ApplicationUser user, IReadOnlyList<string> roles)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(roles);

        return new UserDto(
            Id: user.Id,
            UserName: user.UserName ?? string.Empty,
            Email: user.Email ?? string.Empty,
            BirthDate: user.BirthDate,
            BirthTime: user.BirthTime,
            BirthPlace: user.BirthPlace,
            AscendantSignId: user.AscendantSignId,
            SunSignId: user.SunSignId,
            IsActive: user.IsActive,
            CreatedAt: user.CreatedAt,
            Roles: roles);
    }
}
