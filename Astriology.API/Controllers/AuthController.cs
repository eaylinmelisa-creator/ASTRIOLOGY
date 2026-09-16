using Astriology.API.Extensions;
using Astriology.Application.DTOs.Auth;
using Astriology.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Astriology.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IUserService _userService;

    public AuthController(IAuthService authService, IUserService userService)
    {
        _authService = authService;
        _userService = userService;
    }

    /// <summary>Creates an account in the User role and signs it in.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting(ServiceCollectionExtensions.AuthRateLimitPolicy)]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken) =>
        (await _authService.RegisterAsync(request, cancellationToken)).ToActionResult();

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(ServiceCollectionExtensions.AuthRateLimitPolicy)]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken) =>
        (await _authService.LoginAsync(request, cancellationToken)).ToActionResult();

    /// <summary>
    /// Issues a new access token. The refresh token is returned unchanged - there is
    /// no rotation in this project (roadmap section 5.6).
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(ServiceCollectionExtensions.AuthRateLimitPolicy)]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(RefreshRequest request, CancellationToken cancellationToken) =>
        (await _authService.RefreshAsync(request, cancellationToken)).ToActionResult();

    /// <summary>Revokes the refresh token. Idempotent: logging out twice is not an error.</summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(RefreshRequest request, CancellationToken cancellationToken) =>
        (await _authService.LogoutAsync(request, cancellationToken)).ToActionResult();

    /// <summary>The signed-in user's own account.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken) =>
        (await _userService.GetByIdAsync(User.GetUserId(), cancellationToken)).ToActionResult();

    /// <summary>Changes the signed-in user's password. The current one is always required.</summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordRequest request,
        CancellationToken cancellationToken) =>
        (await _authService.ChangePasswordAsync(User.GetUserId(), request, cancellationToken))
            .ToActionResult();
}
