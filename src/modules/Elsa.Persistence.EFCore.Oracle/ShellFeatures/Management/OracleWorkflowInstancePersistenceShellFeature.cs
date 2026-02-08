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
    DisplayName = "Oracle Workflow Instance Persistence",
    Description = "Provides Oracle persistence for workflow instances",
    DependsOn = ["WorkflowManagement", "WorkflowInstances"])]
[UsedImplicitly]
public class OracleWorkflowInstancePersistenceShellFeature
    : EFCoreWorkflowInstancePersistenceShellFeatureBase
{
    /// <inheritdoc />
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options)
    {
        builder.UseElsaOracle(migrationsAssembly, connectionString, options);
    }
}
