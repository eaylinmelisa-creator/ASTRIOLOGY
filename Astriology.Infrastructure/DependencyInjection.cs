using Astriology.Application.Interfaces;
using Astriology.Domain.Constants;
using Astriology.Infrastructure.Identity;
using Astriology.Infrastructure.Persistence;
using Astriology.Infrastructure.Security;
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
                // Values come from AccountRules so Identity and the request validators
                // cannot drift apart.
                options.Password.RequiredLength = AccountRules.PasswordMinLength;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = true;

                options.User.AllowedUserNameCharacters = AccountRules.AllowedUserNameCharacters;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<AstriologyDbContext>();

        // Validated here so a missing or too-short signing key crashes the application
        // at boot instead of on the first login attempt.
        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .Validate(
                settings => !string.IsNullOrWhiteSpace(settings.Issuer),
                "Jwt:Issuer is required.")
            .Validate(
                settings => !string.IsNullOrWhiteSpace(settings.Audience),
                "Jwt:Audience is required.")
            .Validate(
                settings => !string.IsNullOrWhiteSpace(settings.Key),
                "Jwt:Key is required. Set it through user secrets (Jwt:Key) or the Jwt__Key "
                + "environment variable - never in a tracked configuration file.")
            .Validate(
                settings => settings.Key.Length >= JwtSettings.MinimumKeyLength,
                $"Jwt:Key must be at least {JwtSettings.MinimumKeyLength} characters so the "
                + "signing key reaches the 256 bits HMAC-SHA256 requires.")
            .Validate(
                settings => settings.AccessTokenMinutes > 0,
                "Jwt:AccessTokenMinutes must be greater than zero.")
            .Validate(
                settings => settings.RefreshTokenDays > 0,
                "Jwt:RefreshTokenDays must be greater than zero.")
            .ValidateOnStart();

        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();

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
