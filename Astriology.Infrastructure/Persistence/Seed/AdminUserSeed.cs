using Astriology.Domain.Constants;
using Astriology.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace Astriology.Infrastructure.Persistence.Seed;

/// <summary>
/// Creates the single administrator account. Registration always grants the User
/// role, so this is the only way an Admin ever comes into existence.
/// </summary>
/// <remarks>
/// Credentials come from configuration, never from a literal in the source or from a
/// tracked configuration file. The user name and email live in
/// appsettings.Development.json; the password is supplied out of band:
/// <c>dotnet user-secrets set "Seed:AdminPassword" "..." --project Astriology.API</c>
/// locally, or the <c>Seed__AdminPassword</c> environment variable in Docker.
/// The credentials are documented in the README, but the password itself is never
/// committed.
/// </remarks>
internal static class AdminUserSeed
{
    public static async Task<int> SeedAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration)
    {
        var userName = Required(configuration, "Seed:AdminUserName");

        var existing = await userManager.FindByNameAsync(userName);
        if (existing is not null)
        {
            return existing.Id;
        }

        // Read only once the account actually has to be created, so an already-seeded
        // database starts without the password being configured at all.
        var email = Required(configuration, "Seed:AdminEmail");
        var password = Required(configuration, "Seed:AdminPassword");

        var admin = new ApplicationUser
        {
            UserName = userName,
            Email = email,
            EmailConfirmed = true,
            BirthDate = new DateOnly(1990, 6, 15),
            BirthTime = new TimeOnly(10, 30),
            BirthPlace = "İstanbul",
        };

        var created = await userManager.CreateAsync(admin, password);
        if (!created.Succeeded)
        {
            throw new InvalidOperationException(
                $"Could not create the administrator account: {IdentityErrors.Describe(created)}");
        }

        var roleAssigned = await userManager.AddToRoleAsync(admin, RoleNames.Admin);
        if (!roleAssigned.Succeeded)
        {
            throw new InvalidOperationException(
                $"Could not assign the Admin role: {IdentityErrors.Describe(roleAssigned)}");
        }

        return admin.Id;
    }

    private static string Required(IConfiguration configuration, string key) =>
        configuration[key]
        ?? throw new InvalidOperationException(
            $"Configuration key '{key}' is missing. The administrator account cannot be seeded without it.");
}
