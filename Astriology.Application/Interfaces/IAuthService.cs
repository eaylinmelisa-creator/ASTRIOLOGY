using Astriology.Application.Common;
using Astriology.Application.DTOs.Auth;

namespace Astriology.Application.Interfaces;

/// <summary>
/// Registration, sign-in and token lifecycle.
/// </summary>
/// <remarks>
/// Implemented in the infrastructure layer because every operation goes through
/// Identity's UserManager. Wrapping UserManager in yet another abstraction would
/// repeat the mistake the roadmap avoids for EF Core: re-implementing a framework
/// abstraction that already does the job.
/// </remarks>
public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    /// <summary>Issues a new access token. The refresh token itself is unchanged.</summary>
    Task<Result<AuthResponse>> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken);

    Task<Result> LogoutAsync(RefreshRequest request, CancellationToken cancellationToken);

    Task<Result> ChangePasswordAsync(
        int userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken);
}
