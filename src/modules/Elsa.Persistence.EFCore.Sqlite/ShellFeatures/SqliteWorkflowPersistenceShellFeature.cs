using CShells.Features;
using Elsa.Persistence.EFCore.Sqlite.ShellFeatures.Management;
using Elsa.Persistence.EFCore.Sqlite.ShellFeatures.Runtime;
using Elsa.Persistence.EFCore;
using Elsa.Platform.PackageManifest.Generator.Hints;
using JetBrains.Annotations;

namespace Elsa.Persistence.EFCore.Sqlite.ShellFeatures;

/// <summary>
/// Configures the persistence feature to use Sqlite persistence for all workflow data.
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
///         "SqliteWorkflowPersistence": {
///           "ConnectionString": "Data Source=elsa.db;Cache=Shared"
///         }
///       },
///       "Features": ["Elsa", "SqliteWorkflowPersistence"]
///     }]
///   }
/// }
/// </code>
/// </example>
/// <para>
/// Individual features can still override settings if needed by providing feature-specific configuration:
/// </para>
/// <example>
/// <code>
/// {
///   "CShells": {
///     "Shells": [{
///       "Settings": {
///         "SqliteWorkflowPersistence": {
///           "ConnectionString": "Data Source=elsa.db;Cache=Shared"
///         },
///         "SqliteWorkflowRuntimePersistence": {
///           "ConnectionString": "Data Source=elsa_runtime.db;Cache=Shared"
///         }
///       },
///       "Features": ["Elsa", "SqliteWorkflowPersistence"]
///     }]
///   }
/// }
/// </code>
/// </example>
/// </remarks>
[ShellFeature(
    DisplayName = "Sqlite Workflow Persistence",
    Description = "Provides Sqlite persistence for workflow definitions, instances, and runtime data with unified configuration",
    DependsOn = [typeof(SqliteWorkflowDefinitionPersistenceShellFeature), typeof(SqliteWorkflowInstancePersistenceShellFeature), typeof(SqliteWorkflowRuntimePersistenceShellFeature)])]
[UsedImplicitly]
[ManifestInfrastructure("sqlite-database", "database", Reason = "Stores workflow definitions, instances, and runtime data in SQLite.", Providers = new[] { "SQLite" }, ConfigurationKeys = new[] { "ConnectionString" })]
public class SqliteWorkflowPersistenceShellFeature : CombinedPersistenceShellFeatureBase
{
}