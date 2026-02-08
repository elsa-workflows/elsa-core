using System.Reflection;
using CShells.Features;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.SqlServer.ShellFeatures.Management;

/// <summary>
/// Configures the management feature to use SqlServer persistence.
/// </summary>
[ShellFeature(
    DisplayName = "SqlServer Workflow Definition Persistence",
    Description = "Provides SqlServer persistence for workflow definitions",
    DependsOn = ["WorkflowManagement", "WorkflowDefinitions"])]
[UsedImplicitly]
public class SqlServerWorkflowDefinitionPersistenceShellFeature
    : EFCoreWorkflowDefinitionPersistenceShellFeatureBase
{
    /// <inheritdoc />
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options)
    {
        builder.UseElsaSqlServer(migrationsAssembly, connectionString, options);
    }
}
