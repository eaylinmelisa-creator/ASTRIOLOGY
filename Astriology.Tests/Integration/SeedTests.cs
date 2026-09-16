using Astriology.Domain.Constants;
using Astriology.Infrastructure.Identity;
using Astriology.Infrastructure.Persistence;
using Astriology.Tests.Fixtures;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Astriology.Tests.Integration;

[Collection(TestDatabaseCollection.Name)]
public sealed class SeedTests
{
    private readonly TestDatabaseFixture _fixture;

    public SeedTests(TestDatabaseFixture fixture) => _fixture = fixture;

    private static CancellationToken Token => CancellationToken.None;

    [Fact]
    public async Task Seed_creates_the_expected_reference_data()
    {
        await using var scope = _fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AstriologyDbContext>();

        Assert.Equal(12, await context.ZodiacSigns.CountAsync(Token));
        Assert.Equal(4, await context.Categories.CountAsync(Token));
        Assert.Equal(48, await context.Posts.CountAsync(Token));
        Assert.Equal(2, await context.Roles.CountAsync(Token));

        // Not an exact count: the auth tests register accounts of their own into this
        // same database. That the two seeded accounts exist is asserted by
        // Seeded_accounts_hold_the_roles_they_are_supposed_to.
        Assert.True(await context.Users.CountAsync(Token) >= 2);
    }

    [Fact]
    public async Task Seeded_accounts_hold_the_roles_they_are_supposed_to()
    {
        await using var scope = _fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var admin = await userManager.FindByNameAsync("admin");
        var demo = await userManager.FindByNameAsync("demouser");

        Assert.NotNull(admin);
        Assert.NotNull(demo);
        Assert.Contains(RoleNames.Admin, await userManager.GetRolesAsync(admin));
        Assert.Contains(RoleNames.User, await userManager.GetRolesAsync(demo));

        // The password must be hashed, never stored as given.
        Assert.NotNull(admin.PasswordHash);
        Assert.NotEqual(_fixture.AdminPassword, admin.PasswordHash);
        Assert.True(await userManager.CheckPasswordAsync(admin, _fixture.AdminPassword));
    }

    [Fact]
    public async Task Running_the_seed_again_inserts_nothing()
    {
        int signsBefore, categoriesBefore, postsBefore, usersBefore, rolesBefore;

        await using (var scope = _fixture.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AstriologyDbContext>();
            signsBefore = await context.ZodiacSigns.CountAsync(Token);
            categoriesBefore = await context.Categories.CountAsync(Token);
            postsBefore = await context.Posts.CountAsync(Token);
            usersBefore = await context.Users.CountAsync(Token);
            rolesBefore = await context.Roles.CountAsync(Token);
        }

        await DbInitializer.InitializeAsync(_fixture.RootServices, Token);

        await using (var scope = _fixture.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AstriologyDbContext>();
            Assert.Equal(signsBefore, await context.ZodiacSigns.CountAsync(Token));
            Assert.Equal(categoriesBefore, await context.Categories.CountAsync(Token));
            Assert.Equal(postsBefore, await context.Posts.CountAsync(Token));
            Assert.Equal(usersBefore, await context.Users.CountAsync(Token));
            Assert.Equal(rolesBefore, await context.Roles.CountAsync(Token));
        }
    }
}
