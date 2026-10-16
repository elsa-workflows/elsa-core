using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Elsa.Persistence.EFCore.UnitTests;

/// <summary>
/// The V3_6 runtime migrations create the StoredTrigger unique index over four columns.
/// Provider snapshots and designers must describe that same index so
/// <see cref="IMigrationsModelDiffer"/> reports no unique-index operations for it.
/// </summary>
/// <remarks>
/// Other pending snapshot gaps (V3_7 Scheduling* indexes, Oracle NCLOB vs NVARCHAR2 mappings) are
/// out of scope here. Those diffs remain, so this test does not require
/// <see cref="DatabaseFacade.HasPendingModelChanges"/> to be globally false.
/// </remarks>
public class StoredTriggerRuntimeModelSnapshotTests
{
    private static readonly string[] UniqueIndexColumns =
    [
        "WorkflowDefinitionId",
        "Hash",
        "ActivityId",
        "TenantId"
    ];

    [Fact]
    public void PostgreSql_RuntimeModel_MatchesSnapshot_AndHasFourColumnUniqueIndex()
    {
        RuntimeModelSnapshotInspector.AssertEmptyStoredTriggerUniqueIndexDiff(ConfigurePostgreSql);
        RuntimeModelSnapshotInspector.AssertStoredTriggerUniqueIndex(ConfigurePostgreSql, UniqueIndexColumns);
    }

    [Fact]
    public void Sqlite_RuntimeModel_MatchesSnapshot_AndHasFourColumnUniqueIndex()
    {
        RuntimeModelSnapshotInspector.AssertEmptyStoredTriggerUniqueIndexDiff(ConfigureSqlite);
        RuntimeModelSnapshotInspector.AssertStoredTriggerUniqueIndex(ConfigureSqlite, UniqueIndexColumns);
    }

    [Fact]
    public void Oracle_RuntimeModel_MatchesSnapshot_AndHasFourColumnUniqueIndex()
    {
        RuntimeModelSnapshotInspector.AssertEmptyStoredTriggerUniqueIndexDiff(ConfigureOracle);
        RuntimeModelSnapshotInspector.AssertStoredTriggerUniqueIndex(
            ConfigureOracle,
            UniqueIndexColumns,
            expectedFilter: "\"Hash\" IS NOT NULL AND \"TenantId\" IS NOT NULL");
    }

    [Fact]
    public void SqlServer_RuntimeModel_MatchesSnapshot_AndHasFourColumnUniqueIndex()
    {
        RuntimeModelSnapshotInspector.AssertEmptyStoredTriggerUniqueIndexDiff(ConfigureSqlServer);
        RuntimeModelSnapshotInspector.AssertStoredTriggerUniqueIndex(
            ConfigureSqlServer,
            UniqueIndexColumns,
            expectedFilter: "[Hash] IS NOT NULL");
    }

    private static void ConfigurePostgreSql(DbContextOptionsBuilder<RuntimeElsaDbContext> builder) =>
        builder.UseElsaPostgreSql(typeof(Elsa.Persistence.EFCore.PostgreSql.Migrations.Runtime.V3_6).Assembly, "Host=unused");

    private static void ConfigureSqlite(DbContextOptionsBuilder<RuntimeElsaDbContext> builder) =>
        builder.UseElsaSqlite(typeof(Elsa.Persistence.EFCore.Sqlite.Migrations.Runtime.V3_6).Assembly, "Data Source=:memory:");

    private static void ConfigureOracle(DbContextOptionsBuilder<RuntimeElsaDbContext> builder) =>
        builder.UseElsaOracle(typeof(Elsa.Persistence.EFCore.Oracle.Migrations.Runtime.V3_6).Assembly, "Data Source=unused");

    [Fact]
    public void UseElsaSqlServer_DoesNotAppendModelConfiguration()
    {
        var options = new ElsaDbContextOptions().ConfigureSqlServer();
        var before = options.GetModelConfigurations(typeof(RuntimeElsaDbContext)).GetInvocationList().Length;

        options.ConfigureSqlServer();
        Assert.Equal(before, options.GetModelConfigurations(typeof(RuntimeElsaDbContext)).GetInvocationList().Length);

        ConfigureSqlServer(new DbContextOptionsBuilder<RuntimeElsaDbContext>(), options);
        ConfigureSqlServer(new DbContextOptionsBuilder<RuntimeElsaDbContext>(), options);

        Assert.Equal(before, options.GetModelConfigurations(typeof(RuntimeElsaDbContext)).GetInvocationList().Length);
    }

    private static void ConfigureSqlServer(DbContextOptionsBuilder<RuntimeElsaDbContext> builder) =>
        ConfigureSqlServer(builder, new ElsaDbContextOptions().ConfigureSqlServer());

    private static void ConfigureSqlServer(DbContextOptionsBuilder<RuntimeElsaDbContext> builder, ElsaDbContextOptions options) =>
        builder.UseElsaSqlServer(typeof(Elsa.Persistence.EFCore.SqlServer.Migrations.Runtime.V3_6).Assembly, "Server=unused", options);
}
