using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Elsa.Persistence.EFCore.UnitTests;

/// <summary>
/// V3_7 already created the Scheduling* indexes, and Oracle V3_6 already converted unbounded
/// strings to NCLOB. The runtime snapshot (and the current Oracle model) must describe that
/// same schema so <see cref="Microsoft.EntityFrameworkCore.Migrations.IMigrationsModelDiffer"/>
/// does not emit a spurious CreateIndex or AlterColumn.
/// </summary>
/// <remarks>
/// Other pending snapshot gaps (for example the StoredTrigger unique-index columns addressed
/// by #8868) are out of scope. This test does not require
/// <see cref="Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade.HasPendingModelChanges"/>
/// to be globally false.
/// </remarks>
public class RuntimeSnapshotDriftTests
{
    private static readonly string[] SchedulingIndexNames =
    [
        "IX_ActivityExecutionRecord_SchedulingActivityExecutionId",
        "IX_ActivityExecutionRecord_SchedulingActivityId",
        "IX_ActivityExecutionRecord_SchedulingWorkflowInstanceId"
    ];

    [Fact]
    public void PostgreSql_RuntimeModel_HasNoSchedulingCreateIndex()
    {
        AssertNoSchedulingCreateIndex(ConfigurePostgreSql);
    }

    [Fact]
    public void Sqlite_RuntimeModel_HasNoSchedulingCreateIndex()
    {
        AssertNoSchedulingCreateIndex(ConfigureSqlite);
    }

    [Fact]
    public void SqlServer_RuntimeModel_HasNoSchedulingCreateIndex()
    {
        AssertNoSchedulingCreateIndex(ConfigureSqlServer);
    }

    [Fact]
    public void Oracle_RuntimeModel_HasNoSchedulingCreateIndex_AndNoAlterColumn()
    {
        var operations = RuntimeModelDiffer.GetDifferences(ConfigureOracle);

        AssertNoSchedulingCreateIndex(operations);
        var alterColumns = operations.OfType<AlterColumnOperation>().ToArray();

        Assert.True(
            alterColumns.Length == 0,
            "Expected no Oracle AlterColumn operations against the runtime snapshot, but found:"
            + Environment.NewLine
            + RuntimeModelDiffer.Format(alterColumns));
    }

    private static void AssertNoSchedulingCreateIndex(Action<DbContextOptionsBuilder<RuntimeElsaDbContext>> configure) =>
        AssertNoSchedulingCreateIndex(RuntimeModelDiffer.GetDifferences(configure));

    private static void AssertNoSchedulingCreateIndex(IReadOnlyList<MigrationOperation> operations)
    {
        var createIndexes = operations
            .OfType<CreateIndexOperation>()
            .Where(operation => SchedulingIndexNames.Contains(operation.Name, StringComparer.Ordinal))
            .ToArray();

        Assert.True(
            createIndexes.Length == 0,
            "Expected no Scheduling* CreateIndex operations against the runtime snapshot, but found:"
            + Environment.NewLine
            + RuntimeModelDiffer.Format(createIndexes));
    }

    private static void ConfigurePostgreSql(DbContextOptionsBuilder<RuntimeElsaDbContext> builder) =>
        builder.UseElsaPostgreSql(typeof(Elsa.Persistence.EFCore.PostgreSql.Migrations.Runtime.V3_6).Assembly, "Host=unused");

    private static void ConfigureSqlite(DbContextOptionsBuilder<RuntimeElsaDbContext> builder) =>
        builder.UseElsaSqlite(typeof(Elsa.Persistence.EFCore.Sqlite.Migrations.Runtime.V3_6).Assembly, "Data Source=:memory:");

    private static void ConfigureSqlServer(DbContextOptionsBuilder<RuntimeElsaDbContext> builder) =>
        builder.UseElsaSqlServer(typeof(Elsa.Persistence.EFCore.SqlServer.Migrations.Runtime.V3_6).Assembly, "Server=unused");

    private static void ConfigureOracle(DbContextOptionsBuilder<RuntimeElsaDbContext> builder) =>
        builder.UseElsaOracle(typeof(Elsa.Persistence.EFCore.Oracle.Migrations.Runtime.V3_6).Assembly, "Data Source=unused");
}
