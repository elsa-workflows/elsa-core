using CShells.Features;
using Elsa.Persistence.EFCore.SqlServer.ShellFeatures.Management;
using Elsa.Persistence.EFCore.SqlServer.ShellFeatures.Runtime;
using Elsa.Persistence.EFCore;
using Elsa.Platform.PackageManifest.Generator.Hints;
using JetBrains.Annotations;

namespace Elsa.Persistence.EFCore.SqlServer.ShellFeatures;

/// <summary>
/// Configures the persistence feature to use SQL Server persistence for all workflow data.
/// 
/// This combined feature provides unified configuration for workflow definitions, instances,
/// and runtime persistence. Instead of configuring each dependent feature separately,
/// you can configure this feature once and all dependent features will use the shared settings.
/// </summary>
/// <remarks>
/// <example>
/// Example appsettings.json:
/// <code>
/// {
///   "CShells": {
///     "Shells": [{
///       "Settings": {
///         "SqlServerWorkflowPersistence": {
///           "ConnectionString": "Server=localhost;Database=Elsa;Integrated Security=true;TrustServerCertificate=true"
///         }
///       },
///       "Features": ["Elsa", "SqlServerWorkflowPersistence"]
///     }]
///   }
/// }
/// </code>
/// </example>
/// </remarks>
[ShellFeature(
    DisplayName = "SQL Server Workflow Persistence",
    Description = "Provides SQL Server persistence for workflow definitions, instances, and runtime data with unified configuration",
    DependsOn = [typeof(SqlServerWorkflowDefinitionPersistenceShellFeature), typeof(SqlServerWorkflowInstancePersistenceShellFeature), typeof(SqlServerWorkflowRuntimePersistenceShellFeature)])]
[UsedImplicitly]
[ManifestInfrastructure("sqlserver-database", "database", Reason = "Stores workflow definitions, instances, and runtime data in SQL Server.", Providers = new[] { "SQL Server" }, ConfigurationKeys = new[] { "ConnectionString" })]
public class SqlServerWorkflowPersistenceShellFeature : CombinedPersistenceShellFeatureBase
{
}

