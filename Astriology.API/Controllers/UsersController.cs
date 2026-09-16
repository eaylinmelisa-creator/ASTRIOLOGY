using Astriology.API.Extensions;
using Astriology.Application.Common;
using Astriology.Application.DTOs.Auth;
using Astriology.Application.DTOs.Users;
using Astriology.Application.Interfaces;
using Astriology.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Astriology.API.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private const int DefaultPageSize = 20;

    private readonly IUserService _userService;

    public UsersController(IUserService userService) => _userService = userService;

    /// <summary>Lists accounts, newest page first. Administrators only.</summary>
    [HttpGet]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType<PagedResult<UserDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = DefaultPageSize) =>
        (await _userService.GetPagedAsync(page, pageSize, cancellationToken)).ToActionResult();

    /// <summary>
    /// Updates the caller's own profile. The id comes from the token, never from the
    /// request, so this route cannot be used to edit somebody else.
    /// </summary>
    [HttpPut("me")]
    [Authorize]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateOwnProfile(
        UpdateProfileRequest request,
        CancellationToken cancellationToken) =>
        (await _userService.UpdateProfileAsync(User.GetUserId(), request, cancellationToken))
            .ToActionResult();

    /// <summary>
    /// Soft-deletes an account: the row and the user's comments survive, but the
    /// account can no longer sign in (roadmap section 5.4).
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        // An administrator deactivating themselves would lock the only Admin account
        // out of the system, and nothing in the app can create another one.
        if (id == User.GetUserId())
        {
            return Result.Conflict("Kendi hesabınızı pasife alamazsınız.").ToActionResult();
        }

        return (await _userService.DeactivateAsync(id, cancellationToken)).ToActionResult();
    }
}
