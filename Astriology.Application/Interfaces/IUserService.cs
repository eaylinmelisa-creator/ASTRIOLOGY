using Astriology.Application.Common;
using Astriology.Application.DTOs.Auth;
using Astriology.Application.DTOs.Users;

namespace Astriology.Application.Interfaces;

/// <summary>
/// Reading and maintaining accounts. This is the only way the application layer
/// reaches users, which is what keeps Identity types out of it (roadmap section 3,
/// "Identity İstisnası").
/// </summary>
public interface IUserService
{
    Task<Result<UserDto>> GetByIdAsync(int userId, CancellationToken cancellationToken);

    Task<Result<PagedResult<UserDto>>> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<Result<UserDto>> UpdateProfileAsync(
        int userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Soft delete: sets IsActive to false. The row and the user's comments survive,
    /// and the author is shown as "Silinmiş kullanıcı" (roadmap section 5.4).
    /// </summary>
    Task<Result> DeactivateAsync(int userId, CancellationToken cancellationToken);
}
