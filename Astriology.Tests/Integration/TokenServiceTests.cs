using System.IdentityModel.Tokens.Jwt;
using Astriology.Application.Interfaces;
using Astriology.Domain.Constants;
using Astriology.Infrastructure.Security;
using Astriology.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Astriology.Tests.Integration;

/// <summary>
/// Token contents and lifetimes. Resolved from the container because TokenService
/// needs the database for the refresh half, even though access token creation does
/// not touch it.
/// </summary>
[Collection(TestDatabaseCollection.Name)]
public sealed class TokenServiceTests
{
    private readonly TestDatabaseFixture _fixture;

    public TokenServiceTests(TestDatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public void Access_token_carries_the_subject_name_email_and_roles()
    {
        using var scope = _fixture.CreateScope();
        var tokens = scope.ServiceProvider.GetRequiredService<ITokenService>();

        var issued = tokens.CreateAccessToken(42, "deneme_kullanici", "deneme@astriology.local", [RoleNames.Admin]);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(issued.Value);

        Assert.Equal("42", jwt.Claims.Single(claim => claim.Type == TokenClaimNames.Subject).Value);
        Assert.Equal(
            "deneme_kullanici",
            jwt.Claims.Single(claim => claim.Type == TokenClaimNames.UserName).Value);
        Assert.Equal(
            "deneme@astriology.local",
            jwt.Claims.Single(claim => claim.Type == TokenClaimNames.Email).Value);
        Assert.Equal(RoleNames.Admin, jwt.Claims.Single(claim => claim.Type == TokenClaimNames.Role).Value);
    }

    [Fact]
    public void Access_token_expires_two_hours_after_it_is_issued()
    {
        using var scope = _fixture.CreateScope();
        var tokens = scope.ServiceProvider.GetRequiredService<ITokenService>();

        var before = DateTime.Now;
        var issued = tokens.CreateAccessToken(1, "kullanici", "kullanici@astriology.local", []);

        var lifetime = issued.ExpiresAt - before;

        // Two hours is the deliberate choice recorded in roadmap section 5.6.
        Assert.InRange(lifetime.TotalMinutes, 119, 121);
    }

    [Fact]
    public void Access_token_carries_every_role_the_user_holds()
    {
        using var scope = _fixture.CreateScope();
        var tokens = scope.ServiceProvider.GetRequiredService<ITokenService>();

        var issued = tokens.CreateAccessToken(
            7,
            "cift_rollu",
            "cift@astriology.local",
            [RoleNames.Admin, RoleNames.User]);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(issued.Value);
        var roles = jwt.Claims
            .Where(claim => claim.Type == TokenClaimNames.Role)
            .Select(claim => claim.Value)
            .ToList();

        Assert.Contains(RoleNames.Admin, roles);
        Assert.Contains(RoleNames.User, roles);
    }

    [Fact]
    public void Access_token_carries_no_birth_details()
    {
        using var scope = _fixture.CreateScope();
        var tokens = scope.ServiceProvider.GetRequiredService<ITokenService>();

        var issued = tokens.CreateAccessToken(1, "kullanici", "kullanici@astriology.local", []);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(issued.Value);

        // Anyone holding the token can read it, so it stays limited to what
        // authorization needs.
        Assert.DoesNotContain(
            jwt.Claims,
            claim => claim.Type.Contains("birth", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Refresh_token_expires_fifteen_days_after_it_is_created()
    {
        await using var scope = _fixture.CreateScope();
        var tokens = scope.ServiceProvider.GetRequiredService<ITokenService>();

        var before = DateTime.Now;
        var issued = await tokens.CreateRefreshTokenAsync(1, CancellationToken.None);

        Assert.InRange((issued.ExpiresAt - before).TotalDays, 14.9, 15.1);
    }

    [Fact]
    public async Task Refresh_tokens_are_unique_per_issue()
    {
        await using var scope = _fixture.CreateScope();
        var tokens = scope.ServiceProvider.GetRequiredService<ITokenService>();

        var first = await tokens.CreateRefreshTokenAsync(1, CancellationToken.None);
        var second = await tokens.CreateRefreshTokenAsync(1, CancellationToken.None);

        Assert.NotEqual(first.Value, second.Value);
    }
}
