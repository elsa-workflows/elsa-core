using System.Reflection;
using CShells.Features;
using Elsa.Platform.PackageManifest.Generator.Hints;
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
    DependsOn = [typeof(global::Elsa.Workflows.Management.ShellFeatures.WorkflowManagementFeature), typeof(global::Elsa.Workflows.Management.ShellFeatures.WorkflowInstancesFeature)])]
[UsedImplicitly]
[ManifestInfrastructure("oracle-database", "database", Reason = "Stores workflow instances in Oracle Database.", Providers = new[] { "Oracle" }, ConfigurationKeys = new[] { "ConnectionString" })]
public class OracleWorkflowInstancePersistenceShellFeature
    : EFCoreWorkflowInstancePersistenceShellFeatureBase
{
    /// <inheritdoc />
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options)
    {
        options = options.ConfigureOracle();
        builder.UseElsaOracle(migrationsAssembly, connectionString, options);
    }
}
