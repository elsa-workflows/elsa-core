using System.Reflection;
using CShells.Features;
using Elsa.Platform.PackageManifest.Generator.Hints;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.SqlServer.ShellFeatures.Management;

/// <summary>
/// Configures the management feature to use SqlServer persistence.
/// </summary>
[ShellFeature(
    DisplayName = "SqlServer Workflow Instance Persistence",
    Description = "Provides SqlServer persistence for workflow instances",
    DependsOn = [typeof(global::Elsa.Workflows.Management.ShellFeatures.WorkflowManagementFeature), typeof(global::Elsa.Workflows.Management.ShellFeatures.WorkflowInstancesFeature)])]
[UsedImplicitly]
[ManifestInfrastructure("sqlserver-database", "database", Reason = "Stores workflow instances in SQL Server.", Providers = new[] { "SQL Server" }, ConfigurationKeys = new[] { "ConnectionString" })]
public class SqlServerWorkflowInstancePersistenceShellFeature
    : EFCoreWorkflowInstancePersistenceShellFeatureBase
{
    /// <inheritdoc />
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options)
    {
        builder.UseElsaSqlServer(migrationsAssembly, connectionString, options);
    }
}
