using Dapper;
using Elsa.Common.Multitenancy;
using Elsa.Common.Serialization;
using Elsa.Persistence.Dapper.Contracts;
using Elsa.Persistence.Dapper.Extensions;
using Elsa.Persistence.Dapper.Modules.Management.Records;
using Elsa.Persistence.Dapper.Modules.Management.Stores;
using Elsa.Persistence.Dapper.Services;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Serialization.Serializers;
using FluentMigrator.Runner;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.Dapper.UnitTests;

public sealed class DapperWorkflowDefinitionVersionMigrationTests
{
    private const string PostgreSqlConnectionVariable = "ELSA_DAPPER_MANAGEMENT_POSTGRES_TEST_CONNECTION";
    private const string SqlServerConnectionVariable = "ELSA_DAPPER_MANAGEMENT_SQLSERVER_TEST_CONNECTION";
    private const long PreviousMigration = 10005;
    private const long UniqueVersionMigration = 10006;

    [Fact]
    public async Task SqliteEnforcesTenantLocalVersionsWithoutRewritingLegacyRows()
    {
        var file = Path.Combine(Path.GetTempPath(), $"elsa-dapper-versions-{Guid.NewGuid():N}.db");
        var connectionString = new SqliteConnectionStringBuilder { DataSource = file, Pooling = false }.ToString();

        try
        {
            await VerifyMigrationAsync(connectionString, new SqliteDbConnectionProvider(connectionString), builder => builder.AddSQLite());
        }
        finally
        {
            File.Delete(file);
        }
    }

    [NativeDatabaseFact(PostgreSqlConnectionVariable)]
    public Task PostgreSqlEnforcesTenantLocalVersionsWithoutRewritingLegacyRows()
    {
        var connectionString = Environment.GetEnvironmentVariable(PostgreSqlConnectionVariable)!;
        return VerifyMigrationAsync(connectionString, new PostgreSqlDbConnectionProvider(connectionString), builder => builder.AddPostgres());
    }

    [NativeDatabaseFact(SqlServerConnectionVariable)]
    public Task SqlServerEnforcesTenantLocalVersionsWithoutRewritingLegacyRows()
    {
        var connectionString = Environment.GetEnvironmentVariable(SqlServerConnectionVariable)!;
        return VerifyMigrationAsync(connectionString, new SqlServerDbConnectionProvider(connectionString), builder => builder.AddSqlServer());
    }

    private static async Task VerifyMigrationAsync(string connectionString, IDbConnectionProvider connectionProvider, Action<IMigrationRunnerBuilder> configureProvider)
    {
        await using var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton(SerializationTypeRegistry.CreateDefault())
            .AddFluentMigratorCore()
            .ConfigureRunner(builder =>
            {
                configureProvider(builder);
                builder.WithGlobalConnectionString(connectionString).ScanIn(typeof(Migrations.Management.V3_10_WorkflowDefinitionVersion).Assembly).For.Migrations();
            }).BuildServiceProvider();
        using var scope = services.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        var tenant = new DefaultTenantAccessor();
        var store = new DapperWorkflowDefinitionStore(new Store<WorkflowDefinitionRecord>(connectionProvider, tenant, "WorkflowDefinitions"),
            new JsonPayloadSerializer(scope.ServiceProvider), connectionProvider);
        var definitionId = Guid.NewGuid().ToString("N");
        var legacy = Definition("default-" + definitionId, definitionId, "retained");
        var named = Definition("named-" + definitionId, definitionId, "named");
        var duplicate = Definition("duplicate-" + definitionId, definitionId, "duplicate");

        runner.MigrateUp(PreviousMigration);
        Assert.True(await store.TryAddAsync(legacy));
        using (tenant.PushContext(new Tenant { Id = "named" }))
            Assert.True(await store.TryAddAsync(named));

        runner.MigrateUp(UniqueVersionMigration);
        using (tenant.PushContext(Tenant.Default))
            Assert.False(await store.TryAddAsync(duplicate));
        Assert.False(await store.TryAddAsync(legacy));
        Assert.Equal("retained", (await store.FindAsync(new() { Id = legacy.Id }))?.StringData);
        using (tenant.PushContext(new Tenant { Id = "named" }))
            Assert.Equal("named", (await store.FindAsync(new() { Id = named.Id }))?.StringData);

        var firstVersion = new[]
        {
            Definition("first-a-" + definitionId, "first-" + definitionId, "a"),
            Definition("first-b-" + definitionId, "first-" + definitionId, "b")
        };
        var results = await Task.WhenAll(firstVersion.Select(record => Task.Run(() => store.TryAddAsync(record))));
        Assert.Single(results, result => result);
        Assert.Equal(firstVersion[Array.IndexOf(results, true)].StringData,
            (await store.FindAsync(new() { DefinitionId = "first-" + definitionId }))?.StringData);

        runner.MigrateDown(PreviousMigration);
        using (tenant.PushContext(Tenant.Default))
            Assert.True(await store.TryAddAsync(duplicate));

        var exception = Record.Exception(() => runner.MigrateUp(UniqueVersionMigration));
        Assert.NotNull(exception);
        Assert.Contains("Tenant (default) has 2 rows", exception.ToString());
        Assert.Contains(definitionId, exception.ToString());
        Assert.Contains("No workflow definition rows were changed", exception.ToString());
        Assert.Equal("retained", (await store.FindAsync(new() { Id = legacy.Id }))?.StringData);
        Assert.Equal("duplicate", (await store.FindAsync(new() { Id = duplicate.Id }))?.StringData);

        var quote = connectionProvider.Dialect.QuoteIdentifier;
        using (var connection = connectionProvider.GetConnection())
        {
            var table = quote("WorkflowDefinitions");
            var idColumn = quote("Id");
            var tenantColumn = quote("TenantId");
            Assert.Null(await connection.QuerySingleAsync<string?>($"SELECT {tenantColumn} FROM {table} WHERE {idColumn} = @Id", new { legacy.Id }));
            Assert.Equal(string.Empty, await connection.QuerySingleAsync<string?>($"SELECT {tenantColumn} FROM {table} WHERE {idColumn} = @Id", new { duplicate.Id }));
            await connection.ExecuteAsync($"DELETE FROM {table} WHERE {idColumn} = @Id", new { duplicate.Id });
        }

        runner.MigrateUp(UniqueVersionMigration);
        Assert.False(await store.TryAddAsync(duplicate));

        var retainedPublication = (await store.FindAsync(new() { Id = legacy.Id }))!;
        retainedPublication.IsPublished = true;
        Assert.True(await store.TryPublishAsync(retainedPublication, versions => versions.Count == 1 && !versions.Single().IsPublished));

        var newPublication = retainedPublication.ShallowClone();
        newPublication.Id = "published-" + definitionId;
        newPublication.Version = 2;
        newPublication.StringData = "new publication";
        Assert.True(await store.TryPublishAsync(newPublication, versions => versions.Count == 1 && versions.Single().IsPublished));

        var retained = (await store.FindAsync(new() { Id = legacy.Id }))!;
        Assert.Null(retained.TenantId);
        Assert.False(retained.IsPublished);
        Assert.False(retained.IsLatest);
        var newest = (await store.FindAsync(new() { Id = newPublication.Id }))!;
        Assert.True(newest.IsPublished);
        Assert.True(newest.IsLatest);
        Assert.Equal("new publication", newest.StringData);

        var plan = new WorkflowDefinitionDeletionPlan([newest], [retained, newest]);
        Assert.True(await store.TryDeleteAsync(plan));
        Assert.Null(await store.FindAsync(new() { Id = newest.Id }));
        var promoted = (await store.FindAsync(new() { Id = retained.Id }))!;
        Assert.True(promoted.IsLatest);
        Assert.Null(promoted.TenantId);
    }

    private static WorkflowDefinition Definition(string id, string definitionId, string data) => new()
    {
        Id = id,
        DefinitionId = definitionId,
        Version = 1,
        IsLatest = true,
        StringData = data,
        CreatedAt = DateTimeOffset.UnixEpoch,
        MaterializerName = "test"
    };

    public sealed class NativeDatabaseFactAttribute : FactAttribute
    {
        public NativeDatabaseFactAttribute(string connectionVariable)
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(connectionVariable)))
                Skip = $"Set {connectionVariable} to a disposable database.";
        }
    }
}
