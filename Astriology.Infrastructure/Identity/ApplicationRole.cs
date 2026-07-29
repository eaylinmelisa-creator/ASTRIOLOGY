using Microsoft.AspNetCore.Identity;

namespace Astriology.Infrastructure.Identity;

/// <summary>
/// Identity role with an int key. Only the two seeded roles exist; there is no
/// role management UI.
/// </summary>
public sealed class ApplicationRole : IdentityRole<int>
{
    public ApplicationRole()
    {
    }

    public ApplicationRole(string roleName)
        : base(roleName)
    {
    }
}
