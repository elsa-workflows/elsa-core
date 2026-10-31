using Elsa.Common.Multitenancy;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Workflows.Management.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Elsa.Persistence.EFCore.UnitTests;

/// <summary>
/// #8994: Oracle stores <c>''</c> as <c>NULL</c>, so the default tenant cannot be inserted into the
/// NOT NULL PK that #8391 created on <c>WorkflowDefinitionRegistryGenerations.TenantId</c>.
/// </summary>
/// <remarks>
/// Scripts are generated offline through <see cref="IMigrator.GenerateScript"/>; no Oracle connection is opened.
/// A live <c>gvenzl/oracle-free</c> container is not available in this environment (Docker is not running).
/// </remarks>
public class OracleDefaultTenantNullabilityTests
{
    private const string RegistryGenerationMigration = "20260924222221_WorkflowDefinitionRegistryGeneration";
    private const string NullableDefaultTenantMigration = "20261022120000_NullableDefaultTenant";

    [Theory]
    [InlineData("Elsa", MigrationsSqlGenerationOptions.Default)]
    [InlineData("Elsa", MigrationsSqlGenerationOptions.Idempotent)]
    [InlineData("custom_schema", MigrationsSqlGenerationOptions.Default)]
    public void NewOracleMigration_AltersRegistryGenerationTenantIdToNullable(string schema, MigrationsSqlGenerationOptions options)
    {
        var script = GenerateManagementScript(RegistryGenerationMigration, NullableDefaultTenantMigration, schema, options);

        Assert.Contains("ALTER TABLE", script, StringComparison.Ordinal);
        Assert.Contains("MODIFY", script, StringComparison.Ordinal);
        Assert.Contains("\"TenantId\"", script, StringComparison.Ordinal);
        Assert.Contains("NVL(\"TenantId\", CHR(1))", script, StringComparison.Ordinal);
        Assert.Contains("PK_WorkflowDefinitionRegistryGenerations", script, StringComparison.Ordinal);
        Assert.Contains("\"Id\"", script, StringComparison.Ordinal);
        Assert.DoesNotContain("CREATE TABLE", script, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Elsa", MigrationsSqlGenerationOptions.Default)]
    [InlineData("Elsa", MigrationsSqlGenerationOptions.Idempotent)]
    [InlineData("custom_schema", MigrationsSqlGenerationOptions.Default)]
    public void NewOracleMigration_DownChecksNullTenantBeforeDroppingNvlIndex(string schema, MigrationsSqlGenerationOptions options)
    {
        var script = GenerateManagementScript(NullableDefaultTenantMigration, RegistryGenerationMigration, schema, options);
        var ensure = script.IndexOf("RAISE_APPLICATION_ERROR(-20006", StringComparison.Ordinal);
        var drop = script.IndexOf("IX_WorkflowDefinitionRegistryGenerations_TenantId", StringComparison.Ordinal);

        Assert.True(ensure >= 0, "Down script must emit EnsureNoNullValues (ORA-20006).\n" + script);
        Assert.True(drop >= 0, "Down script must drop the NVL unique index.\n" + script);
        Assert.True(ensure < drop, "EnsureNoNullValues must run before DROP INDEX so a failed rollback keeps uniqueness.\n" + script);

        var migration = MigrationScriptGenerator.CreateMigration<ManagementElsaDbContext>(
            builder => builder.UseElsaOracle(
                typeof(Elsa.Persistence.EFCore.Oracle.Migrations.Management.NullableDefaultTenant).Assembly,
                "Data Source=unused",
                new ElsaDbContextOptions { SchemaName = schema }),
            NullableDefaultTenantMigration);
        var sql = migration.DownOperations.OfType<SqlOperation>().ToList();
        var ensureOp = sql.FindIndex(operation => operation.Sql.Contains("RAISE_APPLICATION_ERROR(-20006", StringComparison.Ordinal));
        var dropOp = sql.FindIndex(operation => operation.Sql.Contains("IX_WorkflowDefinitionRegistryGenerations_TenantId", StringComparison.Ordinal));

        Assert.True(ensureOp >= 0 && dropOp >= 0 && ensureOp < dropOp);
    }

    [Fact]
    public void OtherProviders_HaveNoNullableDefaultTenantManagementMigration()
    {
        AssertNoNullableDefaultTenantMigration(typeof(Elsa.Persistence.EFCore.Sqlite.Migrations.Management.WorkflowDefinitionRegistryGeneration).Assembly);
        AssertNoNullableDefaultTenantMigration(typeof(Elsa.Persistence.EFCore.SqlServer.Migrations.Management.WorkflowDefinitionRegistryGeneration).Assembly);
        AssertNoNullableDefaultTenantMigration(typeof(Elsa.Persistence.EFCore.PostgreSql.Migrations.Management.WorkflowDefinitionRegistryGeneration).Assembly);
#if NET9_0
        AssertNoNullableDefaultTenantMigration(typeof(Elsa.Persistence.EFCore.MySql.Migrations.Management.WorkflowDefinitionRegistryGeneration).Assembly);
#endif
    }

    [Fact]
    public void Oracle_ManagementModel_HasNoPendingDiffAgainstSnapshot()
    {
        var operations = ManagementModelDiffer.GetDifferences(ConfigureOracle);
        var registryOperations = operations
            .Where(operation => operation switch
            {
                Microsoft.EntityFrameworkCore.Migrations.Operations.AlterColumnOperation alter =>
                    alter.Table == "WorkflowDefinitionRegistryGenerations",
                Microsoft.EntityFrameworkCore.Migrations.Operations.AddColumnOperation add =>
                    add.Table == "WorkflowDefinitionRegistryGenerations",
                Microsoft.EntityFrameworkCore.Migrations.Operations.DropColumnOperation drop =>
                    drop.Table == "WorkflowDefinitionRegistryGenerations",
                Microsoft.EntityFrameworkCore.Migrations.Operations.AddPrimaryKeyOperation addKey =>
                    addKey.Table == "WorkflowDefinitionRegistryGenerations",
                Microsoft.EntityFrameworkCore.Migrations.Operations.DropPrimaryKeyOperation dropKey =>
                    dropKey.Table == "WorkflowDefinitionRegistryGenerations",
                Microsoft.EntityFrameworkCore.Migrations.Operations.CreateIndexOperation create =>
                    create.Table == "WorkflowDefinitionRegistryGenerations",
                Microsoft.EntityFrameworkCore.Migrations.Operations.DropIndexOperation dropIndex =>
                    dropIndex.Table == "WorkflowDefinitionRegistryGenerations",
                _ => false
            })
            .ToArray();

        Assert.True(
            registryOperations.Length == 0,
            "Expected no pending model operations for WorkflowDefinitionRegistryGenerations, but found:"
            + Environment.NewLine
            + RuntimeModelDiffer.Format(registryOperations));
    }

    [Fact]
    public void Oracle_RegistryGenerationTenantId_IsOptionalAndWritesEmptyStringAsNull()
    {
        using var dbContext = ManagementModelDiffer.Create(ConfigureOracle);
        var entity = dbContext.Model.FindEntityType(typeof(WorkflowDefinitionRegistryGeneration));
        Assert.NotNull(entity);

        var tenantId = entity.FindProperty(nameof(WorkflowDefinitionRegistryGeneration.TenantId));
        Assert.NotNull(tenantId);
        Assert.False(tenantId.IsKey());
        Assert.True(tenantId.IsNullable);

        var converter = tenantId.GetValueConverter();
        Assert.NotNull(converter);
        Assert.Null(converter.ConvertToProvider(Tenant.DefaultTenantId));
        Assert.Null(converter.ConvertToProvider(string.Empty));
        Assert.Equal("acme", converter.ConvertToProvider("acme"));

        var id = entity.FindProperty("Id");
        Assert.NotNull(id);
        Assert.True(id.IsPrimaryKey());

        Assert.Null(dbContext.Model.FindEntityType(typeof(WorkflowDefinition))?.FindProperty("TenantId")?.GetValueConverter());
        Assert.Null(dbContext.Model.FindEntityType(typeof(WorkflowInstance))?.FindProperty("TenantId")?.GetValueConverter());
    }

    [Fact]
    public void Oracle_DefaultTenantLookup_TranslatesToIsNull()
    {
        using var dbContext = ManagementModelDiffer.Create(ConfigureOracle);
        const string namedTenant = "acme";

        var defaultSql = dbContext.WorkflowDefinitionRegistryGenerations
            .Where(x => x.TenantId == Tenant.DefaultTenantId)
            .ToQueryString();
        var namedSql = dbContext.WorkflowDefinitionRegistryGenerations
            .Where(x => x.TenantId == namedTenant)
            .ToQueryString();

        Assert.Contains("IS NULL", defaultSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"TenantId\" =", defaultSql, StringComparison.Ordinal);
        Assert.DoesNotContain("IS NULL", namedSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"TenantId\" =", namedSql, StringComparison.Ordinal);
    }

    private static void AssertNoNullableDefaultTenantMigration(System.Reflection.Assembly assembly)
    {
        Assert.DoesNotContain(
            assembly.GetTypes().Select(type => type.Name),
            name => name.Contains("NullableDefaultTenant", StringComparison.Ordinal));
    }

    private static void ConfigureOracle(DbContextOptionsBuilder<ManagementElsaDbContext> builder) =>
        builder.UseElsaOracle(
            typeof(Elsa.Persistence.EFCore.Oracle.Migrations.Management.NullableDefaultTenant).Assembly,
            "Data Source=unused",
            new ElsaDbContextOptions().ConfigureOracle());

    private static string GenerateManagementScript(string fromMigration, string toMigration, string schema, MigrationsSqlGenerationOptions options) =>
        MigrationScriptGenerator.Generate<ManagementElsaDbContext>(
            builder => builder.UseElsaOracle(
                typeof(Elsa.Persistence.EFCore.Oracle.Migrations.Management.NullableDefaultTenant).Assembly,
                "Data Source=unused",
                new ElsaDbContextOptions { SchemaName = schema }),
            fromMigration,
            toMigration,
            options);
}
