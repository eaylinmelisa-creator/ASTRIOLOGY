using Astriology.Infrastructure.Identity;
using Astriology.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Astriology.Infrastructure;

/// <summary>
/// Single entry point for registering this layer. The API calls
/// <see cref="AddInfrastructure"/> and never references EF Core or Identity types
/// directly.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Characters allowed in a user name. Turkish letters are excluded.</summary>
    private const string AllowedUserNameCharacters =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Fail at startup rather than on the first request that touches the database.
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Connection string 'Default' is missing. Set ConnectionStrings:Default in "
                + "appsettings.Development.json, user secrets, or the ConnectionStrings__Default "
                + "environment variable.");

        services.AddDbContext<AstriologyDbContext>(options => options.UseSqlServer(connectionString));

        // AddIdentityCore rather than AddIdentity: this is a token-based API and must not
        // pull in Identity's cookie authentication handlers.
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = true;

                options.User.AllowedUserNameCharacters = AllowedUserNameCharacters;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<AstriologyDbContext>();

        // Deliberately not registered:
        // - AddSignInManager: SignInManager lives in the ASP.NET Core shared framework,
        //   which this class library does not reference. Password checks in Faz 2 use
        //   UserManager.CheckPasswordAsync instead, keeping this layer free of a web
        //   framework dependency.
        // - AddDefaultTokenProviders: only needed for email confirmation and password
        //   reset tokens, both out of scope (roadmap section 5.8).

        return services;
    }
}
