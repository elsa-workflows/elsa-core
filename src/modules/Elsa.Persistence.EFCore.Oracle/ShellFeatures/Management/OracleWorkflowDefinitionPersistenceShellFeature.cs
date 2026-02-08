using System.Reflection;
using CShells.Features;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.Oracle.ShellFeatures.Management;

/// <summary>
/// Configures the management feature to use Oracle persistence.
/// </summary>
[ShellFeature(
    DisplayName = "Oracle Workflow Definition Persistence",
    Description = "Provides Oracle persistence for workflow definitions",
    DependsOn = ["WorkflowManagement", "WorkflowDefinitions"])]
[UsedImplicitly]
public class OracleWorkflowDefinitionPersistenceShellFeature
    : EFCoreWorkflowDefinitionPersistenceShellFeatureBase
{
    /// <inheritdoc />
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options)
    {
        builder.UseElsaOracle(migrationsAssembly, connectionString, options);
    }
}
