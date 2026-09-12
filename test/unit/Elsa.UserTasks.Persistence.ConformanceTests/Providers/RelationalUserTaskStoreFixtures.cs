using System.Reflection;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.UserTasks.Persistence.ConformanceTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.UserTasks.Persistence.ConformanceTests.Providers;

/// <summary>SQLite runs unconditionally: it is the relational provider CI covers on every pull request.</summary>
public sealed class SqliteUserTaskStoreFixture : EFCoreUserTaskStoreFixture
{
    private readonly string _databasePath = Path.Join(Path.GetTempPath(), $"elsa-user-tasks-conformance-{Guid.NewGuid():N}.db");

    public SqliteUserTaskStoreFixture() : base(ConformanceProviders.Sqlite)
    {
    }

    protected override bool DropsOwnDatabase => true;
    protected override Assembly MigrationsAssembly => typeof(UserTasks.Persistence.EFCore.Sqlite.Extensions.SqliteUserTasksPersistenceFeatureExtensions).Assembly;
    protected override string ResolveConnectionString() => $"Data Source={_databasePath}";
    protected override void ConfigureServices(IServiceCollection services) => services.AddSqliteEntityModelCreatingHandlers();
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, string connectionString) => builder.UseElsaSqlite(MigrationsAssembly, connectionString);
}

/// <summary>
/// SQL Server, PostgreSQL, and Oracle run only when an operator points the matching environment variable at
/// a disposable database. The suite migrates that database and isolates itself by tenant; it never drops it.
/// </summary>
public sealed class SqlServerUserTaskStoreFixture : EFCoreUserTaskStoreFixture
{
    public SqlServerUserTaskStoreFixture() : base(ConformanceProviders.SqlServer)
    {
    }

    protected override Assembly MigrationsAssembly => typeof(UserTasks.Persistence.EFCore.SqlServer.Extensions.SqlServerUserTasksPersistenceFeatureExtensions).Assembly;
    protected override string ResolveConnectionString() => Provider.ConnectionString;
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, string connectionString) => builder.UseElsaSqlServer(MigrationsAssembly, connectionString);
}

public sealed class PostgreSqlUserTaskStoreFixture : EFCoreUserTaskStoreFixture
{
    public PostgreSqlUserTaskStoreFixture() : base(ConformanceProviders.PostgreSql)
    {
    }

    protected override Assembly MigrationsAssembly => typeof(UserTasks.Persistence.EFCore.PostgreSql.Extensions.PostgreSqlUserTasksPersistenceFeatureExtensions).Assembly;
    protected override string ResolveConnectionString() => Provider.ConnectionString;
    protected override void ConfigureServices(IServiceCollection services) => services.AddPostgreSqlEntityModelCreatingHandlers();
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, string connectionString) => builder.UseElsaPostgreSql(MigrationsAssembly, connectionString);
}

public sealed class OracleUserTaskStoreFixture : EFCoreUserTaskStoreFixture
{
    public OracleUserTaskStoreFixture() : base(ConformanceProviders.Oracle)
    {
    }

    protected override Assembly MigrationsAssembly => typeof(UserTasks.Persistence.EFCore.Oracle.Extensions.OracleUserTasksPersistenceFeatureExtensions).Assembly;
    protected override string ResolveConnectionString() => Provider.ConnectionString;
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, string connectionString) => builder.UseElsaOracle(MigrationsAssembly, connectionString);
}
