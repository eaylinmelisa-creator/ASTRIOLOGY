using Astriology.Domain.Constants;
using Astriology.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace Astriology.Infrastructure.Persistence.Seed;

/// <summary>
/// Creates one ordinary account in the User role so the reader-facing features can
/// be tried without registering first.
/// </summary>
/// <remarks>
/// This account has no privileges beyond what registration grants; it exists purely
/// for review convenience. Credentials follow the same rule as the administrator:
/// name and email in configuration, password supplied out of band through user
/// secrets or the <c>Seed__DemoPassword</c> environment variable.
/// </remarks>
internal static class DemoUserSeed
{
    public static async Task SeedAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration)
    {
        var userName = Required(configuration, "Seed:DemoUserName");

        if (await userManager.FindByNameAsync(userName) is not null)
        {
            return;
        }

        var email = Required(configuration, "Seed:DemoEmail");
        var password = Required(configuration, "Seed:DemoPassword");

        var demoUser = new ApplicationUser
        {
            UserName = userName,
            Email = email,
            EmailConfirmed = true,
            BirthDate = new DateOnly(1998, 11, 3),
            BirthTime = new TimeOnly(21, 15),
            BirthPlace = "Ankara",
        };

        var created = await userManager.CreateAsync(demoUser, password);
        if (!created.Succeeded)
        {
            throw new InvalidOperationException(
                $"Could not create the demo account: {IdentityErrors.Describe(created)}");
        }

        var roleAssigned = await userManager.AddToRoleAsync(demoUser, RoleNames.User);
        if (!roleAssigned.Succeeded)
        {
            throw new InvalidOperationException(
                $"Could not assign the User role: {IdentityErrors.Describe(roleAssigned)}");
        }
    }

    private static string Required(IConfiguration configuration, string key) =>
        configuration[key]
        ?? throw new InvalidOperationException(
            $"Configuration key '{key}' is missing. The demo account cannot be seeded without it.");
}
