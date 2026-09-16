using Astriology.Infrastructure.Persistence;
using Astriology.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Astriology.Tests.Integration;

[Collection(TestDatabaseCollection.Name)]
public sealed class SchemaTests
{
    private readonly TestDatabaseFixture _fixture;

    public SchemaTests(TestDatabaseFixture fixture) => _fixture = fixture;

    private static CancellationToken Token => CancellationToken.None;

    [Fact]
    public async Task Migrations_apply_cleanly_and_leave_no_pending_migration()
    {
        await using var scope = _fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AstriologyDbContext>();

        var applied = await context.Database.GetAppliedMigrationsAsync(Token);
        var pending = await context.Database.GetPendingMigrationsAsync(Token);

        Assert.NotEmpty(applied);
        Assert.Empty(pending);
    }

    [Fact]
    public async Task Schema_contains_the_twelve_expected_tables()
    {
        string[] expected =
        [
            "AspNetRoleClaims",
            "AspNetRoles",
            "AspNetUserClaims",
            "AspNetUserLogins",
            "AspNetUserRoles",
            "AspNetUserTokens",
            "AspNetUsers",
            "Categories",
            "Comments",
            "Posts",
            "RefreshTokens",
            "ZodiacSigns",
        ];

        await using var scope = _fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AstriologyDbContext>();

        var actual = await context.Database
            .SqlQuery<string>(
                $"SELECT TABLE_NAME AS Value FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'")
            .ToListAsync(Token);

        // __EFMigrationsHistory is EF's own bookkeeping table and is not part of the model.
        // Ordinal ordering keeps the comparison independent of the machine's culture:
        // Turkish collation orders AspNetUsers before AspNetUserTokens, ordinal does not.
        var modelTables = actual
            .Where(name => name != "__EFMigrationsHistory")
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(expected.OrderBy(name => name, StringComparer.Ordinal), modelTables);
    }
}
