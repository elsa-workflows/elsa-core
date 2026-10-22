using Elsa.Common.Multitenancy;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Workflows.Management.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

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
    public void Oracle_RegistryGenerationTenantId_IsOptionalAndConvertsEmptyStringToNull()
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

        var convertFrom = (Func<string?, string>)converter.ConvertFromProviderExpression.Compile();
        Assert.Equal(Tenant.DefaultTenantId, convertFrom(null));

        var id = entity.FindProperty("Id");
        Assert.NotNull(id);
        Assert.True(id.IsPrimaryKey());
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
