using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Astriology.Tests.Fixtures;

/// <summary>
/// Boots the real API pipeline in memory against AstriologyDbTest.
/// </summary>
/// <remarks>
/// Faz 1 has no endpoints yet, so nothing uses this class so far. It exists now so
/// the HTTP-level tests in Faz 2 start from a factory that already points at the test
/// database instead of the development one.
/// <para>
/// Startup seeding is left switched on: it is guarded and idempotent, and the shared
/// <see cref="TestDatabaseFixture"/> has normally populated the database already.
/// </para>
/// </remarks>
public sealed class AstriologyWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            // Added last so it overrides the API's own appsettings files and points
            // the whole host at the test database.
            configuration.AddJsonFile("appsettings.Testing.json", optional: false);
        });
    }
}
