using Astriology.Application.Common;
using Astriology.Application.DTOs.Auth;
using Astriology.Application.Interfaces;
using Astriology.Domain.Constants;
using Microsoft.AspNetCore.Identity;

namespace Astriology.Infrastructure.Identity;

/// <summary>
/// Registration, sign-in and token lifecycle on top of Identity.
/// </summary>
internal sealed class AuthService : IAuthService
{
    /// <summary>
    /// One message for every sign-in failure. Distinguishing "no such user" from
    /// "wrong password" from "account disabled" would let anyone enumerate accounts.
    /// </summary>
    private const string SignInFailedMessage = "Rumuz veya şifre hatalı.";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;

    public AuthService(UserManager<ApplicationUser> userManager, ITokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = new ApplicationUser
        {
            UserName = request.UserName,
            Email = request.Email,
            BirthDate = request.BirthDate,
            BirthTime = request.BirthTime,
            BirthPlace = request.BirthPlace,
        };

        var created = await _userManager.CreateAsync(user, request.Password);
        if (!created.Succeeded)
        {
            return Result<AuthResponse>.Invalid(Describe(created));
        }

        // Everyone who registers is a User. The single Admin comes from seeding only,
        // and there is no way to ask for a different role (roadmap section 5.5).
        var roleAssigned = await _userManager.AddToRoleAsync(user, RoleNames.User);
        if (!roleAssigned.Succeeded)
        {
            return Result<AuthResponse>.Invalid(Describe(roleAssigned));
        }

        return Result<AuthResponse>.Success(await IssueAsync(user, cancellationToken));
    }

    public async Task<Result<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await _userManager.FindByNameAsync(request.UserName);

        if (user is null
            || !user.IsActive
            || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return Result<AuthResponse>.Unauthorized(SignInFailedMessage);
        }

        return Result<AuthResponse>.Success(await IssueAsync(user, cancellationToken));
    }

    public async Task<Result<AuthResponse>> RefreshAsync(
        RefreshRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var active = await _tokenService.ResolveActiveRefreshTokenAsync(
            request.RefreshToken,
            cancellationToken);

        if (active is null)
        {
            return Result<AuthResponse>.Unauthorized("Yenileme anahtarı geçersiz veya süresi dolmuş.");
        }

        var user = await _userManager.FindByIdAsync(active.UserId.ToString());
        if (user is null || !user.IsActive)
        {
            return Result<AuthResponse>.Unauthorized("Yenileme anahtarı geçersiz veya süresi dolmuş.");
        }

        // No rotation: a new access token is issued and the caller keeps the refresh
        // token it already has (roadmap section 5.6).
        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.CreateAccessToken(
            user.Id,
            user.UserName ?? string.Empty,
            user.Email ?? string.Empty,
            roles);

        return Result<AuthResponse>.Success(new AuthResponse(
            accessToken.Value,
            accessToken.ExpiresAt,
            request.RefreshToken,
            active.ExpiresAt,
            user.ToDto([.. roles])));
    }

    public async Task<Result> LogoutAsync(RefreshRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Idempotent on purpose: reporting "no such token" would confirm to an attacker
        // which values exist, and a client logging out twice is not an error.
        await _tokenService.RevokeRefreshTokenAsync(request.RefreshToken, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> ChangePasswordAsync(
        int userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive)
        {
            return Result.NotFound("Kullanıcı bulunamadı.");
        }

        var changed = await _userManager.ChangePasswordAsync(
            user,
            request.CurrentPassword,
            request.NewPassword);

        return changed.Succeeded
            ? Result.Success()
            : Result.Invalid(Describe(changed));
    }

    private async Task<AuthResponse> IssueAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var roles = await _userManager.GetRolesAsync(user);

        var accessToken = _tokenService.CreateAccessToken(
            user.Id,
            user.UserName ?? string.Empty,
            user.Email ?? string.Empty,
            roles);

        var refreshToken = await _tokenService.CreateRefreshTokenAsync(user.Id, cancellationToken);

        return new AuthResponse(
            accessToken.Value,
            accessToken.ExpiresAt,
            refreshToken.Value,
            refreshToken.ExpiresAt,
            user.ToDto([.. roles]));
    }

    private static string Describe(IdentityResult result) =>
        string.Join(" ", result.Errors.Select(error => error.Description));
}
