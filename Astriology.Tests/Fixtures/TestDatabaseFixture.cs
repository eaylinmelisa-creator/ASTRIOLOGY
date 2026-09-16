using Astriology.Infrastructure;
using Astriology.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Astriology.Tests.Fixtures;

/// <summary>
/// Prepares AstriologyDbTest once per test run: drops whatever is there, applies the
/// migrations and runs the production seed classes.
/// </summary>
/// <remarks>
/// The schema comes from migrations rather than EnsureCreated, so every run also
/// proves the migrations are applicable to an empty database.
/// <para>
/// Seed passwords are generated per run and never stored anywhere. Faz 1 tests do not
/// need them; later phases can read <see cref="AdminPassword"/> and
/// <see cref="DemoPassword"/> to sign in as the seeded accounts.
/// </para>
/// </remarks>
public sealed class TestDatabaseFixture : IAsyncLifetime
{
    private ServiceProvider _serviceProvider = null!;

    /// <summary>Password the seeded administrator was created with, for this run only.</summary>
    public string AdminPassword => TestSecrets.AdminPassword;

    /// <summary>Password the seeded demo user was created with, for this run only.</summary>
    public string DemoPassword => TestSecrets.DemoPassword;

    /// <summary>
    /// Root provider, for the few tests that need to hand the whole container to
    /// something - re-running <see cref="DbInitializer"/>, for instance.
    /// </summary>
    public IServiceProvider RootServices => _serviceProvider;

    public async Task InitializeAsync()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Testing.json", optional: false)
            .AddInMemoryCollection(TestSecrets.AsConfiguration())
            .Build();

        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("appsettings.Testing.json has no 'Default' connection string.");

        GuardAgainstNonTestDatabase(connectionString);

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddInfrastructure(configuration);
        _serviceProvider = services.BuildServiceProvider();

        // xunit 2.x has no ambient test context to take a token from; fixture setup is
        // not cancellable here.
        var cancellationToken = CancellationToken.None;

        await using (var scope = _serviceProvider.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AstriologyDbContext>();
            await context.Database.EnsureDeletedAsync(cancellationToken);
        }

        await DbInitializer.InitializeAsync(_serviceProvider, cancellationToken);
    }

    public async Task DisposeAsync()
    {
        // The database is deliberately left in place after the run so a failure can be
        // inspected. The next run recreates it from scratch.
        await _serviceProvider.DisposeAsync();
    }

    /// <summary>
    /// Opens a fresh context in its own scope. Every test uses its own so a failed
    /// SaveChanges in one test cannot leave tracked entities behind for another.
    /// </summary>
    public AsyncServiceScope CreateScope() => _serviceProvider.CreateAsyncScope();

    /// <summary>
    /// Refuses to run if the connection string does not point at a test database.
    /// InitializeAsync deletes whatever it is given, so a mistyped connection string
    /// would otherwise destroy the development database.
    /// </summary>
    private static void GuardAgainstNonTestDatabase(string connectionString)
    {
        var databaseName = new SqlConnectionStringBuilder(connectionString).InitialCatalog;

        if (!databaseName.EndsWith("Test", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Refusing to run: the test database name must end with 'Test' but was '{databaseName}'. "
                + "The fixture deletes this database, so pointing it at anything else is unsafe.");
        }
    }

}
