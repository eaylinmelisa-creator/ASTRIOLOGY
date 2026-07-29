using Microsoft.AspNetCore.Identity;

namespace Astriology.Infrastructure.Persistence.Seed;

/// <summary>
/// Turns a failed <see cref="IdentityResult"/> into a single readable line for the
/// exception that aborts seeding.
/// </summary>
internal static class IdentityErrors
{
    public static string Describe(IdentityResult result) =>
        string.Join("; ", result.Errors.Select(error => $"{error.Code} - {error.Description}"));
}
