using Elsa.UserTasks.Persistence.ConformanceTests.Infrastructure;
using Elsa.UserTasks.Persistence.ConformanceTests.Providers;

namespace Elsa.UserTasks.Persistence.ConformanceTests;

// One collection per provider. Classes in a collection run sequentially and share the provider's stores,
// so a container-backed provider is migrated once per run; each test still isolates itself by tenant.
// Different providers remain free to run in parallel with each other.

[CollectionDefinition(Name)]
public sealed class InMemoryCollection : ICollectionFixture<InMemoryUserTaskStoreFixture>
{
    public const string Name = "UserTasks:InMemory";
}

[CollectionDefinition(Name)]
public sealed class SqliteCollection : ICollectionFixture<SqliteUserTaskStoreFixture>
{
    public const string Name = "UserTasks:EFCore.Sqlite";
}

[CollectionDefinition(Name)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerUserTaskStoreFixture>
{
    public const string Name = "UserTasks:EFCore.SqlServer";
}

[CollectionDefinition(Name)]
public sealed class PostgreSqlCollection : ICollectionFixture<PostgreSqlUserTaskStoreFixture>
{
    public const string Name = "UserTasks:EFCore.PostgreSql";
}

[CollectionDefinition(Name)]
public sealed class OracleCollection : ICollectionFixture<OracleUserTaskStoreFixture>
{
    public const string Name = "UserTasks:EFCore.Oracle";
}

[CollectionDefinition(Name)]
public sealed class VNextCollection : ICollectionFixture<VNextUserTaskStoreFixture>
{
    public const string Name = "UserTasks:VNext.Sqlite";
}

// ---------------------------------------------------------------------------------------------------
// In-memory: the reference implementation, held to the same contract as the durable providers.
// ---------------------------------------------------------------------------------------------------

[Collection(InMemoryCollection.Name), ConformanceProvider(ConformanceProviders.InMemory)]
public sealed class InMemoryUserTaskRepositoryConformanceTests(InMemoryUserTaskStoreFixture fixture)
    : UserTaskRepositoryConformanceTests(fixture);

[Collection(InMemoryCollection.Name), ConformanceProvider(ConformanceProviders.InMemory)]
public sealed class InMemoryUserTaskGuestSessionConformanceTests(InMemoryUserTaskStoreFixture fixture)
    : UserTaskGuestSessionConformanceTests(fixture);

[Collection(InMemoryCollection.Name), ConformanceProvider(ConformanceProviders.InMemory)]
public sealed class InMemoryUserTaskInvitationOutboxConformanceTests(InMemoryUserTaskStoreFixture fixture)
    : UserTaskInvitationOutboxConformanceTests(fixture);

[Collection(InMemoryCollection.Name), ConformanceProvider(ConformanceProviders.InMemory)]
public sealed class InMemoryUserTaskFaultInjectionConformanceTests(InMemoryUserTaskStoreFixture fixture)
    : UserTaskFaultInjectionConformanceTests(fixture);

// ---------------------------------------------------------------------------------------------------
// EF Core over SQLite: the durable provider CI covers on every pull request.
// ---------------------------------------------------------------------------------------------------

[Collection(SqliteCollection.Name), ConformanceProvider(ConformanceProviders.Sqlite)]
public sealed class SqliteUserTaskRepositoryConformanceTests(SqliteUserTaskStoreFixture fixture)
    : UserTaskRepositoryConformanceTests(fixture);

[Collection(SqliteCollection.Name), ConformanceProvider(ConformanceProviders.Sqlite)]
public sealed class SqliteUserTaskGuestSessionConformanceTests(SqliteUserTaskStoreFixture fixture)
    : UserTaskGuestSessionConformanceTests(fixture);

[Collection(SqliteCollection.Name), ConformanceProvider(ConformanceProviders.Sqlite)]
public sealed class SqliteUserTaskInvitationOutboxConformanceTests(SqliteUserTaskStoreFixture fixture)
    : UserTaskInvitationOutboxConformanceTests(fixture);

[Collection(SqliteCollection.Name), ConformanceProvider(ConformanceProviders.Sqlite)]
public sealed class SqliteUserTaskFaultInjectionConformanceTests(SqliteUserTaskStoreFixture fixture)
    : UserTaskFaultInjectionConformanceTests(fixture);

// ---------------------------------------------------------------------------------------------------
// VNext ships a repository only, so the guest-session and outbox suites deliberately do not run here.
// ---------------------------------------------------------------------------------------------------

[Collection(VNextCollection.Name), ConformanceProvider(ConformanceProviders.VNext)]
public sealed class VNextUserTaskRepositoryConformanceTests(VNextUserTaskStoreFixture fixture)
    : UserTaskRepositoryConformanceTests(fixture);

// ---------------------------------------------------------------------------------------------------
// Container-backed providers. Every test below reports as skipped, with the reason, unless the matching
// environment variable names a disposable database.
// ---------------------------------------------------------------------------------------------------

[Collection(SqlServerCollection.Name), ConformanceProvider(ConformanceProviders.SqlServer)]
public sealed class SqlServerUserTaskRepositoryConformanceTests(SqlServerUserTaskStoreFixture fixture)
    : UserTaskRepositoryConformanceTests(fixture);

[Collection(SqlServerCollection.Name), ConformanceProvider(ConformanceProviders.SqlServer)]
public sealed class SqlServerUserTaskGuestSessionConformanceTests(SqlServerUserTaskStoreFixture fixture)
    : UserTaskGuestSessionConformanceTests(fixture);

[Collection(SqlServerCollection.Name), ConformanceProvider(ConformanceProviders.SqlServer)]
public sealed class SqlServerUserTaskInvitationOutboxConformanceTests(SqlServerUserTaskStoreFixture fixture)
    : UserTaskInvitationOutboxConformanceTests(fixture);

[Collection(SqlServerCollection.Name), ConformanceProvider(ConformanceProviders.SqlServer)]
public sealed class SqlServerUserTaskFaultInjectionConformanceTests(SqlServerUserTaskStoreFixture fixture)
    : UserTaskFaultInjectionConformanceTests(fixture);

[Collection(PostgreSqlCollection.Name), ConformanceProvider(ConformanceProviders.PostgreSql)]
public sealed class PostgreSqlUserTaskRepositoryConformanceTests(PostgreSqlUserTaskStoreFixture fixture)
    : UserTaskRepositoryConformanceTests(fixture);

[Collection(PostgreSqlCollection.Name), ConformanceProvider(ConformanceProviders.PostgreSql)]
public sealed class PostgreSqlUserTaskGuestSessionConformanceTests(PostgreSqlUserTaskStoreFixture fixture)
    : UserTaskGuestSessionConformanceTests(fixture);

[Collection(PostgreSqlCollection.Name), ConformanceProvider(ConformanceProviders.PostgreSql)]
public sealed class PostgreSqlUserTaskInvitationOutboxConformanceTests(PostgreSqlUserTaskStoreFixture fixture)
    : UserTaskInvitationOutboxConformanceTests(fixture);

[Collection(PostgreSqlCollection.Name), ConformanceProvider(ConformanceProviders.PostgreSql)]
public sealed class PostgreSqlUserTaskFaultInjectionConformanceTests(PostgreSqlUserTaskStoreFixture fixture)
    : UserTaskFaultInjectionConformanceTests(fixture);

[Collection(OracleCollection.Name), ConformanceProvider(ConformanceProviders.Oracle)]
public sealed class OracleUserTaskRepositoryConformanceTests(OracleUserTaskStoreFixture fixture)
    : UserTaskRepositoryConformanceTests(fixture);

[Collection(OracleCollection.Name), ConformanceProvider(ConformanceProviders.Oracle)]
public sealed class OracleUserTaskGuestSessionConformanceTests(OracleUserTaskStoreFixture fixture)
    : UserTaskGuestSessionConformanceTests(fixture);

[Collection(OracleCollection.Name), ConformanceProvider(ConformanceProviders.Oracle)]
public sealed class OracleUserTaskInvitationOutboxConformanceTests(OracleUserTaskStoreFixture fixture)
    : UserTaskInvitationOutboxConformanceTests(fixture);

[Collection(OracleCollection.Name), ConformanceProvider(ConformanceProviders.Oracle)]
public sealed class OracleUserTaskFaultInjectionConformanceTests(OracleUserTaskStoreFixture fixture)
    : UserTaskFaultInjectionConformanceTests(fixture);
