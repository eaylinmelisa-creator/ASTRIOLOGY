using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Astriology.Application.Interfaces;
using Astriology.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Astriology.Infrastructure.Security;

/// <summary>
/// Issues access tokens and owns the whole refresh-token lifecycle.
/// </summary>
/// <remarks>
/// Storage, validation and revocation all live here rather than behind a repository:
/// RefreshToken is an infrastructure entity and nothing outside this class needs to
/// know it exists (roadmap section 2.2).
/// </remarks>
internal sealed class TokenService : ITokenService
{
    /// <summary>512 bits of entropy, well beyond guessing range.</summary>
    private const int RefreshTokenByteLength = 64;

    private readonly AstriologyDbContext _context;
    private readonly JwtSettings _settings;

    public TokenService(AstriologyDbContext context, IOptions<JwtSettings> settings)
    {
        _context = context;
        _settings = settings.Value;
    }

    public IssuedToken CreateAccessToken(
        int userId,
        string userName,
        string email,
        IEnumerable<string> roles)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        ArgumentNullException.ThrowIfNull(roles);

        var expiresAt = DateTime.Now.AddMinutes(_settings.AccessTokenMinutes);

        // Only what authorization actually needs. A token is readable by anyone
        // holding it, so birth details and similar data stay out of it.
        var claims = new List<Claim>
        {
            new(TokenClaimNames.Subject, userId.ToString(CultureInfo.InvariantCulture)),
            new(TokenClaimNames.UserName, userName),
            new(TokenClaimNames.Email, email),
            new(TokenClaimNames.TokenId, Guid.NewGuid().ToString("N")),
        };

        claims.AddRange(roles.Select(role => new Claim(TokenClaimNames.Role, role)));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: DateTime.Now,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));

        return new IssuedToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public async Task<IssuedToken> CreateRefreshTokenAsync(int userId, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userId);

        var value = Convert.ToBase64String(RandomNumberGenerator.GetBytes(RefreshTokenByteLength));
        var expiresAt = DateTime.Now.AddDays(_settings.RefreshTokenDays);

        _context.RefreshTokens.Add(new RefreshToken(value, userId, expiresAt));
        await _context.SaveChangesAsync(cancellationToken);

        return new IssuedToken(value, expiresAt);
    }

    public async Task<ActiveRefreshToken?> ResolveActiveRefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        var stored = await _context.RefreshTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(token => token.Token == refreshToken, cancellationToken);

        if (stored is null || !stored.IsActive(DateTime.Now))
        {
            return null;
        }

        return new ActiveRefreshToken(stored.UserId, stored.ExpiresAt);
    }

    public async Task<bool> RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return false;
        }

        var stored = await _context.RefreshTokens
            .SingleOrDefaultAsync(token => token.Token == refreshToken, cancellationToken);

        if (stored is null)
        {
            return false;
        }

        stored.Revoke();
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
