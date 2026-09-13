using Elsa.UserTasks.Persistence.ConformanceTests.Infrastructure;
using Elsa.UserTasks.Persistence.ConformanceTests.Providers;

namespace Elsa.UserTasks.Persistence.ConformanceTests;

// TUnit shares one keyed fixture across a provider's concrete classes and serializes only tests carrying
// that provider's key. A container-backed provider is therefore migrated once while different providers
// remain free to run in parallel.
internal static class ProviderFixtureKeys
{
    public const string InMemory = "UserTasks:InMemory";
    public const string Sqlite = "UserTasks:EFCore.Sqlite";
    public const string SqlServer = "UserTasks:EFCore.SqlServer";
    public const string PostgreSql = "UserTasks:EFCore.PostgreSql";
    public const string Oracle = "UserTasks:EFCore.Oracle";
    public const string VNext = "UserTasks:VNext.Sqlite";
}

// ---------------------------------------------------------------------------------------------------
// In-memory: the reference implementation, held to the same contract as the durable providers.
// ---------------------------------------------------------------------------------------------------

[InheritsTests]
[ClassDataSource<InMemoryUserTaskStoreFixture>(Shared = SharedType.Keyed, Key = ProviderFixtureKeys.InMemory)]
[NotInParallel(ProviderFixtureKeys.InMemory)]
[ConformanceProvider(ConformanceProviders.InMemory)]
public sealed class InMemoryUserTaskRepositoryConformanceTests(InMemoryUserTaskStoreFixture fixture)
    : UserTaskRepositoryConformanceTests(fixture);

[InheritsTests]
[ClassDataSource<InMemoryUserTaskStoreFixture>(Shared = SharedType.Keyed, Key = ProviderFixtureKeys.InMemory)]
[NotInParallel(ProviderFixtureKeys.InMemory)]
[ConformanceProvider(ConformanceProviders.InMemory)]
public sealed class InMemoryUserTaskGuestSessionConformanceTests(InMemoryUserTaskStoreFixture fixture)
    : UserTaskGuestSessionConformanceTests(fixture);

[InheritsTests]
[ClassDataSource<InMemoryUserTaskStoreFixture>(Shared = SharedType.Keyed, Key = ProviderFixtureKeys.InMemory)]
[NotInParallel(ProviderFixtureKeys.InMemory)]
[ConformanceProvider(ConformanceProviders.InMemory)]
public sealed class InMemoryUserTaskInvitationOutboxConformanceTests(InMemoryUserTaskStoreFixture fixture)
    : UserTaskInvitationOutboxConformanceTests(fixture);

[InheritsTests]
[ClassDataSource<InMemoryUserTaskStoreFixture>(Shared = SharedType.Keyed, Key = ProviderFixtureKeys.InMemory)]
[NotInParallel(ProviderFixtureKeys.InMemory)]
[ConformanceProvider(ConformanceProviders.InMemory)]
public sealed class InMemoryUserTaskFaultInjectionConformanceTests(InMemoryUserTaskStoreFixture fixture)
    : UserTaskFaultInjectionConformanceTests(fixture);

// ---------------------------------------------------------------------------------------------------
// EF Core over SQLite: the durable provider CI covers on every pull request.
// ---------------------------------------------------------------------------------------------------

[InheritsTests]
[ClassDataSource<SqliteUserTaskStoreFixture>(Shared = SharedType.Keyed, Key = ProviderFixtureKeys.Sqlite)]
[NotInParallel(ProviderFixtureKeys.Sqlite)]
[ConformanceProvider(ConformanceProviders.Sqlite)]
public sealed class SqliteUserTaskRepositoryConformanceTests(SqliteUserTaskStoreFixture fixture)
    : UserTaskRepositoryConformanceTests(fixture);

[InheritsTests]
[ClassDataSource<SqliteUserTaskStoreFixture>(Shared = SharedType.Keyed, Key = ProviderFixtureKeys.Sqlite)]
[NotInParallel(ProviderFixtureKeys.Sqlite)]
[ConformanceProvider(ConformanceProviders.Sqlite)]
public sealed class SqliteUserTaskGuestSessionConformanceTests(SqliteUserTaskStoreFixture fixture)
    : UserTaskGuestSessionConformanceTests(fixture);

[InheritsTests]
[ClassDataSource<SqliteUserTaskStoreFixture>(Shared = SharedType.Keyed, Key = ProviderFixtureKeys.Sqlite)]
[NotInParallel(ProviderFixtureKeys.Sqlite)]
[ConformanceProvider(ConformanceProviders.Sqlite)]
public sealed class SqliteUserTaskInvitationOutboxConformanceTests(SqliteUserTaskStoreFixture fixture)
    : UserTaskInvitationOutboxConformanceTests(fixture);

[InheritsTests]
[ClassDataSource<SqliteUserTaskStoreFixture>(Shared = SharedType.Keyed, Key = ProviderFixtureKeys.Sqlite)]
[NotInParallel(ProviderFixtureKeys.Sqlite)]
[ConformanceProvider(ConformanceProviders.Sqlite)]
public sealed class SqliteUserTaskFaultInjectionConformanceTests(SqliteUserTaskStoreFixture fixture)
    : UserTaskFaultInjectionConformanceTests(fixture);

// ---------------------------------------------------------------------------------------------------
// VNext ships a repository only, so the guest-session and outbox suites deliberately do not run here.
// ---------------------------------------------------------------------------------------------------

[InheritsTests]
[ClassDataSource<VNextUserTaskStoreFixture>(Shared = SharedType.Keyed, Key = ProviderFixtureKeys.VNext)]
[NotInParallel(ProviderFixtureKeys.VNext)]
[ConformanceProvider(ConformanceProviders.VNext)]
public sealed class VNextUserTaskRepositoryConformanceTests(VNextUserTaskStoreFixture fixture)
    : UserTaskRepositoryConformanceTests(fixture);

// ---------------------------------------------------------------------------------------------------
// Container-backed providers. Every test below reports as skipped, with the reason, unless the matching
// environment variable names a disposable database.
// ---------------------------------------------------------------------------------------------------

[InheritsTests]
[ClassDataSource<SqlServerUserTaskStoreFixture>(Shared = SharedType.Keyed, Key = ProviderFixtureKeys.SqlServer)]
[NotInParallel(ProviderFixtureKeys.SqlServer)]
[ConformanceProvider(ConformanceProviders.SqlServer)]
public sealed class SqlServerUserTaskRepositoryConformanceTests(SqlServerUserTaskStoreFixture fixture)
    : UserTaskRepositoryConformanceTests(fixture);

[InheritsTests]
[ClassDataSource<SqlServerUserTaskStoreFixture>(Shared = SharedType.Keyed, Key = ProviderFixtureKeys.SqlServer)]
[NotInParallel(ProviderFixtureKeys.SqlServer)]
[ConformanceProvider(ConformanceProviders.SqlServer)]
public sealed class SqlServerUserTaskGuestSessionConformanceTests(SqlServerUserTaskStoreFixture fixture)
    : UserTaskGuestSessionConformanceTests(fixture);

[InheritsTests]
[ClassDataSource<SqlServerUserTaskStoreFixture>(Shared = SharedType.Keyed, Key = ProviderFixtureKeys.SqlServer)]
[NotInParallel(ProviderFixtureKeys.SqlServer)]
[ConformanceProvider(ConformanceProviders.SqlServer)]
public sealed class SqlServerUserTaskInvitationOutboxConformanceTests(SqlServerUserTaskStoreFixture fixture)
    : UserTaskInvitationOutboxConformanceTests(fixture);

[InheritsTests]
[ClassDataSource<SqlServerUserTaskStoreFixture>(Shared = SharedType.Keyed, Key = ProviderFixtureKeys.SqlServer)]
[NotInParallel(ProviderFixtureKeys.SqlServer)]
[ConformanceProvider(ConformanceProviders.SqlServer)]
public sealed class SqlServerUserTaskFaultInjectionConformanceTests(SqlServerUserTaskStoreFixture fixture)
    : UserTaskFaultInjectionConformanceTests(fixture);

[InheritsTests]
[ClassDataSource<PostgreSqlUserTaskStoreFixture>(Shared = SharedType.Keyed, Key = ProviderFixtureKeys.PostgreSql)]
[NotInParallel(ProviderFixtureKeys.PostgreSql)]
[ConformanceProvider(ConformanceProviders.PostgreSql)]
public sealed class PostgreSqlUserTaskRepositoryConformanceTests(PostgreSqlUserTaskStoreFixture fixture)
    : UserTaskRepositoryConformanceTests(fixture);

[InheritsTests]
[ClassDataSource<PostgreSqlUserTaskStoreFixture>(Shared = SharedType.Keyed, Key = ProviderFixtureKeys.PostgreSql)]
[NotInParallel(ProviderFixtureKeys.PostgreSql)]
[ConformanceProvider(ConformanceProviders.PostgreSql)]
public sealed class PostgreSqlUserTaskGuestSessionConformanceTests(PostgreSqlUserTaskStoreFixture fixture)
    : UserTaskGuestSessionConformanceTests(fixture);

[InheritsTests]
[ClassDataSource<PostgreSqlUserTaskStoreFixture>(Shared = SharedType.Keyed, Key = ProviderFixtureKeys.PostgreSql)]
[NotInParallel(ProviderFixtureKeys.PostgreSql)]
[ConformanceProvider(ConformanceProviders.PostgreSql)]
public sealed class PostgreSqlUserTaskInvitationOutboxConformanceTests(PostgreSqlUserTaskStoreFixture fixture)
    : UserTaskInvitationOutboxConformanceTests(fixture);

[InheritsTests]
[ClassDataSource<PostgreSqlUserTaskStoreFixture>(Shared = SharedType.Keyed, Key = ProviderFixtureKeys.PostgreSql)]
[NotInParallel(ProviderFixtureKeys.PostgreSql)]
[ConformanceProvider(ConformanceProviders.PostgreSql)]
public sealed class PostgreSqlUserTaskFaultInjectionConformanceTests(PostgreSqlUserTaskStoreFixture fixture)
    : UserTaskFaultInjectionConformanceTests(fixture);

[InheritsTests]
[ClassDataSource<OracleUserTaskStoreFixture>(Shared = SharedType.Keyed, Key = ProviderFixtureKeys.Oracle)]
[NotInParallel(ProviderFixtureKeys.Oracle)]
[ConformanceProvider(ConformanceProviders.Oracle)]
public sealed class OracleUserTaskRepositoryConformanceTests(OracleUserTaskStoreFixture fixture)
    : UserTaskRepositoryConformanceTests(fixture);

[InheritsTests]
[ClassDataSource<OracleUserTaskStoreFixture>(Shared = SharedType.Keyed, Key = ProviderFixtureKeys.Oracle)]
[NotInParallel(ProviderFixtureKeys.Oracle)]
[ConformanceProvider(ConformanceProviders.Oracle)]
public sealed class OracleUserTaskGuestSessionConformanceTests(OracleUserTaskStoreFixture fixture)
    : UserTaskGuestSessionConformanceTests(fixture);

[InheritsTests]
[ClassDataSource<OracleUserTaskStoreFixture>(Shared = SharedType.Keyed, Key = ProviderFixtureKeys.Oracle)]
[NotInParallel(ProviderFixtureKeys.Oracle)]
[ConformanceProvider(ConformanceProviders.Oracle)]
public sealed class OracleUserTaskInvitationOutboxConformanceTests(OracleUserTaskStoreFixture fixture)
    : UserTaskInvitationOutboxConformanceTests(fixture);

[InheritsTests]
[ClassDataSource<OracleUserTaskStoreFixture>(Shared = SharedType.Keyed, Key = ProviderFixtureKeys.Oracle)]
[NotInParallel(ProviderFixtureKeys.Oracle)]
[ConformanceProvider(ConformanceProviders.Oracle)]
public sealed class OracleUserTaskFaultInjectionConformanceTests(OracleUserTaskStoreFixture fixture)
    : UserTaskFaultInjectionConformanceTests(fixture);
