using Astriology.Application.Common;
using Astriology.Application.DTOs.Auth;
using Astriology.Application.DTOs.Users;
using Astriology.Application.Interfaces;
using Astriology.Infrastructure.Persistence;
using Astriology.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Astriology.Tests.Integration;

[Collection(TestDatabaseCollection.Name)]
public sealed class UserServiceTests
{
    private readonly TestDatabaseFixture _fixture;

    public UserServiceTests(TestDatabaseFixture fixture) => _fixture = fixture;

    private static CancellationToken Token => CancellationToken.None;

    [Fact]
    public async Task Deactivate_keeps_the_row_and_blocks_sign_in()
    {
        var request = TestAccounts.NewRegistration();

        await using var scope = _fixture.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var users = scope.ServiceProvider.GetRequiredService<IUserService>();
        var context = scope.ServiceProvider.GetRequiredService<AstriologyDbContext>();

        var registered = await auth.RegisterAsync(request, Token);
        var userId = registered.Value!.User.Id;

        var deactivated = await users.DeactivateAsync(userId, Token);
        Assert.True(deactivated.IsSuccess);

        // Soft delete: the row survives so the user's comments keep an author.
        var stored = await context.Users
            .AsNoTracking()
            .SingleAsync(user => user.Id == userId, Token);

        Assert.False(stored.IsActive);

        var signIn = await auth.LoginAsync(new LoginRequest(request.UserName, request.Password), Token);

        Assert.True(signIn.IsFailure);
        Assert.Equal(ResultError.Unauthorized, signIn.ErrorKind);
    }

    [Fact]
    public async Task Deactivate_revokes_outstanding_refresh_tokens()
    {
        var request = TestAccounts.NewRegistration();

        await using var scope = _fixture.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var users = scope.ServiceProvider.GetRequiredService<IUserService>();

        var registered = await auth.RegisterAsync(request, Token);
        var refreshToken = registered.Value!.RefreshToken;

        await users.DeactivateAsync(registered.Value.User.Id, Token);

        // Otherwise a deactivated account would keep minting access tokens until the
        // refresh token expired on its own.
        var refreshed = await auth.RefreshAsync(new RefreshRequest(refreshToken), Token);

        Assert.True(refreshed.IsFailure);
        Assert.Equal(ResultError.Unauthorized, refreshed.ErrorKind);
    }

    [Fact]
    public async Task Deactivating_an_already_inactive_account_is_a_conflict()
    {
        var request = TestAccounts.NewRegistration();

        await using var scope = _fixture.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var users = scope.ServiceProvider.GetRequiredService<IUserService>();

        var registered = await auth.RegisterAsync(request, Token);
        await users.DeactivateAsync(registered.Value!.User.Id, Token);

        var second = await users.DeactivateAsync(registered.Value.User.Id, Token);

        Assert.True(second.IsFailure);
        Assert.Equal(ResultError.Conflict, second.ErrorKind);
    }

    [Fact]
    public async Task Deactivating_an_unknown_account_is_not_found()
    {
        await using var scope = _fixture.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserService>();

        var result = await users.DeactivateAsync(int.MaxValue, Token);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultError.NotFound, result.ErrorKind);
    }

    [Fact]
    public async Task Get_by_id_returns_the_account_without_any_password_material()
    {
        var request = TestAccounts.NewRegistration();

        await using var scope = _fixture.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var users = scope.ServiceProvider.GetRequiredService<IUserService>();

        var registered = await auth.RegisterAsync(request, Token);

        var result = await users.GetByIdAsync(registered.Value!.User.Id, Token);

        Assert.True(result.IsSuccess);
        Assert.Equal(request.UserName, result.Value!.UserName);
        Assert.Equal(request.Email, result.Value.Email);
        Assert.Equal(request.BirthDate, result.Value.BirthDate);

        // UserDto has no property that could carry a hash; this asserts the shape has
        // not been widened by a later change.
        Assert.DoesNotContain(
            typeof(UserDto).GetProperties(),
            property => property.Name.Contains("Password", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Update_profile_changes_only_the_profile_fields()
    {
        var request = TestAccounts.NewRegistration();

        await using var scope = _fixture.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var users = scope.ServiceProvider.GetRequiredService<IUserService>();

        var registered = await auth.RegisterAsync(request, Token);

        var updated = await users.UpdateProfileAsync(
            registered.Value!.User.Id,
            new UpdateProfileRequest(new DateOnly(1988, 2, 29), new TimeOnly(6, 45), "Bursa"),
            Token);

        Assert.True(updated.IsSuccess);
        Assert.Equal(new DateOnly(1988, 2, 29), updated.Value!.BirthDate);
        Assert.Equal(new TimeOnly(6, 45), updated.Value.BirthTime);
        Assert.Equal("Bursa", updated.Value.BirthPlace);

        // Identity fields are not part of the request and must be untouched.
        Assert.Equal(request.UserName, updated.Value.UserName);
        Assert.Equal(request.Email, updated.Value.Email);
    }

    [Fact]
    public async Task Paged_listing_reports_totals_and_rejects_an_oversized_page()
    {
        await using var scope = _fixture.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserService>();

        var page = await users.GetPagedAsync(1, 2, Token);

        Assert.True(page.IsSuccess);
        Assert.True(page.Value!.Items.Count <= 2);
        // The seeded admin and demo user alone guarantee at least two accounts.
        Assert.True(page.Value.TotalCount >= 2);

        var tooLarge = await users.GetPagedAsync(1, 1000, Token);

        Assert.True(tooLarge.IsFailure);
        Assert.Equal(ResultError.Validation, tooLarge.ErrorKind);
    }
}
