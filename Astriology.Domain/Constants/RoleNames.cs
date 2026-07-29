namespace Astriology.Domain.Constants;

/// <summary>
/// The two roles seeded into Identity. Referenced by authorization attributes and
/// policies so role names are never spelled out as literals at call sites.
/// </summary>
public static class RoleNames
{
    public const string Admin = "Admin";

    public const string User = "User";
}
