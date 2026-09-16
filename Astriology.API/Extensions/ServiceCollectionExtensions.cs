using System.Text;
using System.Threading.RateLimiting;
using Astriology.API.Filters;
using Astriology.API.OpenApi;
using Astriology.Application.Validators;
using Astriology.Infrastructure.Security;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;

namespace Astriology.API.Extensions;

/// <summary>
/// Every presentation-layer registration in one place, so Program.cs reads as a list
/// of capabilities rather than a wall of configuration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Rate limiting policy applied to the authentication endpoints.</summary>
    public const string AuthRateLimitPolicy = "auth";

    /// <summary>CORS policy allowing the SPA origin.</summary>
    public const string CorsPolicy = "spa";

    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddControllers(options => options.Filters.Add<ValidationFilter>());

        // Registers every validator in the Application assembly. Adding a validator is
        // enough for ValidationFilter to start enforcing it.
        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

        services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

        services.AddJwtAuthentication(configuration);

        // No fallback policy: the site is public by default and most endpoints are
        // reads open to everyone. Protection is declared per endpoint with [Authorize],
        // so anything that must be restricted carries the attribute explicitly.
        services.AddAuthorization();

        services.AddAuthRateLimiting();
        services.AddSpaCors(configuration);

        return services;
    }

    private static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException(
                "The 'Jwt' configuration section is missing. See JwtSettings for the required keys.");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Inbound mapping would rewrite "sub" into a long WS-Federation URI and
                // silently break every lookup by claim name.
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = settings.Issuer,
                    ValidAudience = settings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key)),

                    // The five-minute default silently extends every token's life.
                    ClockSkew = TimeSpan.FromSeconds(30),

                    NameClaimType = TokenClaimNames.UserName,
                    RoleClaimType = TokenClaimNames.Role,
                };
            });

        return services;
    }

    /// <summary>
    /// Slows password guessing against the authentication endpoints. Account lockout
    /// was considered and deliberately left out, so this is the only brute-force
    /// defence in place.
    /// </summary>
    private static IServiceCollection AddAuthRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(AuthRateLimitPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    // Per caller, so one client cannot exhaust the budget for everyone.
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromMinutes(1),
                        PermitLimit = 10,
                        QueueLimit = 0,
                    }));
        });

        return services;
    }

    private static IServiceCollection AddSpaCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Explicit origins only. AllowAnyOrigin together with credentials is rejected
        // at runtime and would be wrong here regardless.
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options => options.AddPolicy(CorsPolicy, policy =>
            policy.WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()));

        return services;
    }
}
