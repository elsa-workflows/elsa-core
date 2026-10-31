using System.Reflection;
using CShells.Features;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Workflows.Management.ShellFeatures;
using Elsa.Platform.PackageManifest.Generator.Hints;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.Oracle.ShellFeatures.Management;

/// <summary>
/// Configures the management feature to use Oracle persistence.
/// </summary>
[ManifestFeatureCategory("Persistence")]
[ManifestFeatureCategory("Workflows")]
[ShellFeature(
    DisplayName = "Oracle Workflow Definition Persistence",
    Description = "Provides Oracle persistence for workflow definitions",
    DependsOn = [typeof(WorkflowManagementFeature), typeof(WorkflowDefinitionsFeature)])]
[UsedImplicitly]
[ManifestInfrastructure("oracle-database", "database", Reason = "Stores workflow definitions in Oracle Database.", Providers = new[] { "Oracle" }, ConfigurationKeys = new[] { "ConnectionString" })]
public class OracleWorkflowDefinitionPersistenceShellFeature
    : EFCoreWorkflowDefinitionPersistenceShellFeatureBase
{
    /// <inheritdoc />
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options)
    {
        options = options.ConfigureOracle();
        builder.UseElsaOracle(migrationsAssembly, connectionString, options);
    }
}
