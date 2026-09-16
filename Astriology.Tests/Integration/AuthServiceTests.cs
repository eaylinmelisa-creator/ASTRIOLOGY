using Astriology.Application.Common;
using Astriology.Application.DTOs.Auth;
using Astriology.Application.Interfaces;
using Astriology.Domain.Constants;
using Astriology.Infrastructure.Persistence;
using Astriology.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Astriology.Tests.Integration;

/// <summary>
/// Registration, sign-in and token lifecycle against the real database.
/// </summary>
/// <remarks>
/// These exercise the service layer directly rather than over HTTP: Smart App
/// Control prevents the API assembly from loading into the test host, so status code
/// mapping and middleware are covered by the Swagger smoke tests instead. See the
/// comment in Astriology.Tests.csproj.
/// </remarks>
[Collection(TestDatabaseCollection.Name)]
public sealed class AuthServiceTests
{
    private readonly TestDatabaseFixture _fixture;

    public AuthServiceTests(TestDatabaseFixture fixture) => _fixture = fixture;

    private static CancellationToken Token => CancellationToken.None;

    [Fact]
    public async Task Register_creates_an_account_in_the_User_role()
    {
        var request = TestAccounts.NewRegistration();

        await using var scope = _fixture.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();

        var result = await auth.RegisterAsync(request, Token);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(request.UserName, result.Value.User.UserName);
        Assert.Contains(RoleNames.User, result.Value.User.Roles);
        Assert.DoesNotContain(RoleNames.Admin, result.Value.User.Roles);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.Value.RefreshToken));
    }

    [Fact]
    public async Task Register_stores_the_password_hashed()
    {
        var request = TestAccounts.NewRegistration();

        await using var scope = _fixture.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var context = scope.ServiceProvider.GetRequiredService<AstriologyDbContext>();

        await auth.RegisterAsync(request, Token);

        var stored = await context.Users
            .AsNoTracking()
            .SingleAsync(user => user.UserName == request.UserName, Token);

        Assert.NotNull(stored.PasswordHash);
        Assert.NotEqual(request.Password, stored.PasswordHash);
        Assert.DoesNotContain(request.Password, stored.PasswordHash, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Register_rejects_a_user_name_that_is_already_taken()
    {
        var first = TestAccounts.NewRegistration();

        await using var scope = _fixture.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();

        await auth.RegisterAsync(first, Token);

        // Same name, different email, so the name is unambiguously what fails.
        var duplicate = TestAccounts.NewRegistration(userName: first.UserName);

        var result = await auth.RegisterAsync(duplicate, Token);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultError.Validation, result.ErrorKind);
    }

    [Fact]
    public async Task Register_rejects_an_email_that_is_already_taken()
    {
        var first = TestAccounts.NewRegistration();

        await using var scope = _fixture.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();

        await auth.RegisterAsync(first, Token);

        var duplicate = TestAccounts.NewRegistration(email: first.Email);

        var result = await auth.RegisterAsync(duplicate, Token);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultError.Validation, result.ErrorKind);
    }

    [Fact]
    public async Task Login_succeeds_with_the_right_credentials()
    {
        var request = TestAccounts.NewRegistration();

        await using var scope = _fixture.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();

        await auth.RegisterAsync(request, Token);

        var result = await auth.LoginAsync(new LoginRequest(request.UserName, request.Password), Token);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(request.UserName, result.Value.User.UserName);
    }

    [Fact]
    public async Task Login_fails_with_the_wrong_password()
    {
        var request = TestAccounts.NewRegistration();

        await using var scope = _fixture.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();

        await auth.RegisterAsync(request, Token);

        var result = await auth.LoginAsync(new LoginRequest(request.UserName, "Yanlis1!Sifre"), Token);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultError.Unauthorized, result.ErrorKind);
    }

    [Fact]
    public async Task Login_fails_for_an_unknown_user_with_the_same_message_as_a_wrong_password()
    {
        await using var scope = _fixture.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();

        var unknown = await auth.LoginAsync(
            new LoginRequest($"yok_{Guid.NewGuid():N}"[..20], "Gecerli1!Sifre"),
            Token);

        var request = TestAccounts.NewRegistration();
        await auth.RegisterAsync(request, Token);
        var wrongPassword = await auth.LoginAsync(
            new LoginRequest(request.UserName, "Yanlis1!Sifre"),
            Token);

        // Identical responses, so the API cannot be used to enumerate accounts.
        Assert.Equal(ResultError.Unauthorized, unknown.ErrorKind);
        Assert.Equal(wrongPassword.Error, unknown.Error);
    }

    [Fact]
    public async Task Refresh_issues_a_new_access_token_and_keeps_the_same_refresh_token()
    {
        var request = TestAccounts.NewRegistration();

        await using var scope = _fixture.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();

        var registered = await auth.RegisterAsync(request, Token);
        var original = registered.Value!;

        var refreshed = await auth.RefreshAsync(new RefreshRequest(original.RefreshToken), Token);

        Assert.True(refreshed.IsSuccess);
        // No rotation: the caller keeps the token it already stored.
        Assert.Equal(original.RefreshToken, refreshed.Value!.RefreshToken);
    }

    [Fact]
    public async Task Refresh_rejects_an_expired_token()
    {
        var request = TestAccounts.NewRegistration();

        await using var scope = _fixture.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var context = scope.ServiceProvider.GetRequiredService<AstriologyDbContext>();

        var registered = await auth.RegisterAsync(request, Token);
        var refreshToken = registered.Value!.RefreshToken;

        // Push the expiry into the past through raw SQL: ExpiresAt has a private setter
        // and no behaviour method exists to move it, which is the point.
        await context.Database.ExecuteSqlAsync(
            $"UPDATE RefreshTokens SET ExpiresAt = {DateTime.Now.AddDays(-1)} WHERE Token = {refreshToken}",
            Token);

        var result = await auth.RefreshAsync(new RefreshRequest(refreshToken), Token);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultError.Unauthorized, result.ErrorKind);
    }

    [Fact]
    public async Task Refresh_rejects_an_unknown_token()
    {
        await using var scope = _fixture.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();

        var result = await auth.RefreshAsync(new RefreshRequest(Guid.NewGuid().ToString("N")), Token);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultError.Unauthorized, result.ErrorKind);
    }

    [Fact]
    public async Task Logout_revokes_the_token_and_blocks_further_refreshes()
    {
        var request = TestAccounts.NewRegistration();

        await using var scope = _fixture.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var context = scope.ServiceProvider.GetRequiredService<AstriologyDbContext>();

        var registered = await auth.RegisterAsync(request, Token);
        var refreshToken = registered.Value!.RefreshToken;

        var loggedOut = await auth.LogoutAsync(new RefreshRequest(refreshToken), Token);
        Assert.True(loggedOut.IsSuccess);

        var stored = await context.RefreshTokens
            .AsNoTracking()
            .SingleAsync(token => token.Token == refreshToken, Token);

        Assert.NotNull(stored.RevokedAt);

        var afterLogout = await auth.RefreshAsync(new RefreshRequest(refreshToken), Token);

        Assert.True(afterLogout.IsFailure);
        Assert.Equal(ResultError.Unauthorized, afterLogout.ErrorKind);
    }

    [Fact]
    public async Task Change_password_rejects_a_wrong_current_password()
    {
        var request = TestAccounts.NewRegistration();

        await using var scope = _fixture.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();

        var registered = await auth.RegisterAsync(request, Token);

        var result = await auth.ChangePasswordAsync(
            registered.Value!.User.Id,
            new ChangePasswordRequest("Yanlis1!Sifre", "Yeni1!Sifre", "Yeni1!Sifre"),
            Token);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultError.Validation, result.ErrorKind);
    }

    [Fact]
    public async Task Change_password_succeeds_and_invalidates_the_old_password()
    {
        var request = TestAccounts.NewRegistration();
        const string newPassword = "Yepyeni1!Sifre";

        await using var scope = _fixture.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();

        var registered = await auth.RegisterAsync(request, Token);

        var changed = await auth.ChangePasswordAsync(
            registered.Value!.User.Id,
            new ChangePasswordRequest(request.Password, newPassword, newPassword),
            Token);

        Assert.True(changed.IsSuccess);

        var withOldPassword = await auth.LoginAsync(
            new LoginRequest(request.UserName, request.Password),
            Token);
        Assert.True(withOldPassword.IsFailure);

        var withNewPassword = await auth.LoginAsync(
            new LoginRequest(request.UserName, newPassword),
            Token);
        Assert.True(withNewPassword.IsSuccess);
    }
}
