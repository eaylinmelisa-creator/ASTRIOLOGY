using Astriology.Domain.Constants;
using Astriology.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Astriology.Infrastructure.Persistence.Seed;

/// <summary>
/// Creates the two roles the application recognises. There is no role management
/// UI, so these are the only roles that will ever exist.
/// </summary>
internal static class RoleSeed
{
    public static async Task SeedAsync(RoleManager<ApplicationRole> roleManager)
    {
        foreach (var roleName in new[] { RoleNames.Admin, RoleNames.User })
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(new ApplicationRole(roleName));
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not create role '{roleName}': {DescribeErrors(result)}");
            }
        }
    }

    private static string DescribeErrors(IdentityResult result) =>
        string.Join("; ", result.Errors.Select(error => $"{error.Code} - {error.Description}"));
}
