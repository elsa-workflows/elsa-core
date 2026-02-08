using System.Reflection;
using CShells.Features;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.PostgreSql.ShellFeatures.Management;

/// <summary>
/// Configures the management feature to use PostgreSql persistence.
/// </summary>
[ShellFeature(
    DisplayName = "PostgreSql Workflow Definition Persistence",
    Description = "Provides PostgreSql persistence for workflow definitions",
    DependsOn = ["WorkflowManagement", "WorkflowDefinitions"])]
[UsedImplicitly]
public class PostgreSqlWorkflowDefinitionPersistenceShellFeature
    : EFCoreWorkflowDefinitionPersistenceShellFeatureBase
{
    /// <inheritdoc />
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options)
    {
        builder.UseElsaPostgreSql(migrationsAssembly, connectionString, options);
    }
}
