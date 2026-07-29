using Astriology.Infrastructure.Identity;
using Astriology.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Astriology.Infrastructure.Persistence;

/// <summary>
/// Applies pending migrations and seeds reference data at startup.
/// </summary>
/// <remarks>
/// Every step is guarded, so running this against an already-populated database
/// inserts nothing. It is written for a single application instance; with multiple
/// replicas concurrent migrators would race and a separate migration job would be
/// needed instead.
/// </remarks>
public static class DbInitializer
{
    public static async Task InitializeAsync(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;

        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DbInitializer));
        var context = services.GetRequiredService<AstriologyDbContext>();

        await context.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Database migrations are up to date.");

        await RoleSeed.SeedAsync(services.GetRequiredService<RoleManager<ApplicationRole>>());
        await ZodiacSignSeed.SeedAsync(context, cancellationToken);
        await CategorySeed.SeedAsync(context, cancellationToken);

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = services.GetRequiredService<IConfiguration>();

        // Posts need an author, so the administrator has to exist first.
        var adminId = await AdminUserSeed.SeedAsync(userManager, configuration);
        await DemoUserSeed.SeedAsync(userManager, configuration);

        await PostSeed.SeedAsync(context, adminId, cancellationToken);

        logger.LogInformation(
            "Seeding complete: {SignCount} signs, {CategoryCount} categories, {PostCount} posts.",
            await context.ZodiacSigns.CountAsync(cancellationToken),
            await context.Categories.CountAsync(cancellationToken),
            await context.Posts.CountAsync(cancellationToken));
    }
}
