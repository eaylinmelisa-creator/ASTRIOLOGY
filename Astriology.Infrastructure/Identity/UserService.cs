using Astriology.Application.Common;
using Astriology.Application.DTOs.Auth;
using Astriology.Application.DTOs.Users;
using Astriology.Application.Interfaces;
using Astriology.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Astriology.Infrastructure.Identity;

internal sealed class UserService : IUserService
{
    /// <summary>Caps how much an administrator can pull in one request.</summary>
    private const int MaxPageSize = 100;

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AstriologyDbContext _context;

    public UserService(UserManager<ApplicationUser> userManager, AstriologyDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public async Task<Result<UserDto>> GetByIdAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);

        if (user is null)
        {
            return Result<UserDto>.NotFound("Kullanıcı bulunamadı.");
        }

        var roles = await _userManager.GetRolesAsync(user);

        return Result<UserDto>.Success(user.ToDto([.. roles]));
    }

    public async Task<Result<PagedResult<UserDto>>> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (page < 1)
        {
            return Result<PagedResult<UserDto>>.Invalid("Sayfa numarası 1'den küçük olamaz.");
        }

        if (pageSize is < 1 or > MaxPageSize)
        {
            return Result<PagedResult<UserDto>>.Invalid($"Sayfa boyutu 1 ile {MaxPageSize} arasında olmalıdır.");
        }

        var query = _context.Users.AsNoTracking().OrderBy(user => user.Id);

        var totalCount = await query.CountAsync(cancellationToken);
        var users = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var rolesByUser = await RolesForAsync(users.Select(user => user.Id), cancellationToken);

        var items = users
            .Select(user => user.ToDto(rolesByUser.GetValueOrDefault(user.Id, [])))
            .ToList();

        return Result<PagedResult<UserDto>>.Success(
            new PagedResult<UserDto>(items, totalCount, page, pageSize));
    }

    public async Task<Result<UserDto>> UpdateProfileAsync(
        int userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await _context.Users
            .SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return Result<UserDto>.NotFound("Kullanıcı bulunamadı.");
        }

        user.BirthDate = request.BirthDate;
        user.BirthTime = request.BirthTime;
        user.BirthPlace = request.BirthPlace;

        await _context.SaveChangesAsync(cancellationToken);

        var roles = await _userManager.GetRolesAsync(user);

        return Result<UserDto>.Success(user.ToDto([.. roles]));
    }

    public async Task<Result> DeactivateAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);

        if (user is null)
        {
            return Result.NotFound("Kullanıcı bulunamadı.");
        }

        if (!user.IsActive)
        {
            return Result.Conflict("Kullanıcı zaten pasif durumda.");
        }

        // Soft delete: the row and the user's comments stay, the account can no longer
        // sign in, and the author shows as "Silinmiş kullanıcı" (roadmap section 5.4).
        user.IsActive = false;

        // Existing refresh tokens must stop working immediately, otherwise a deactivated
        // account keeps minting access tokens until they expire.
        var tokens = await _context.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.Revoke();
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    /// <summary>
    /// Loads the roles for a whole page of users in one query rather than calling
    /// UserManager once per user.
    /// </summary>
    private async Task<Dictionary<int, IReadOnlyList<string>>> RolesForAsync(
        IEnumerable<int> userIds,
        CancellationToken cancellationToken)
    {
        var ids = userIds.ToList();

        var pairs = await (
            from userRole in _context.UserRoles
            join role in _context.Roles on userRole.RoleId equals role.Id
            where ids.Contains(userRole.UserId)
            select new { userRole.UserId, role.Name })
            .ToListAsync(cancellationToken);

        return pairs
            .GroupBy(pair => pair.UserId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)[.. group.Select(pair => pair.Name ?? string.Empty)]);
    }
}
