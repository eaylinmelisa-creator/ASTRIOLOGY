namespace Astriology.Tests.Fixtures;

/// <summary>
/// Groups every test that touches the database so the schema is built and seeded
/// once for the whole run instead of once per test class.
/// </summary>
[CollectionDefinition(Name)]
public sealed class TestDatabaseCollection : ICollectionFixture<TestDatabaseFixture>
{
    public const string Name = "Database";
}
