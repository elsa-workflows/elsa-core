using CShells.Features;
using Elsa.Persistence.EFCore.MySql.ShellFeatures.Management;
using Elsa.Persistence.EFCore.MySql.ShellFeatures.Runtime;
using Elsa.Persistence.EFCore;
using Elsa.Platform.PackageManifest.Generator.Hints;
using JetBrains.Annotations;

namespace Elsa.Persistence.EFCore.MySql.ShellFeatures;

/// <summary>
/// Configures the persistence feature to use MySql persistence for all workflow data.
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
///         "MySqlWorkflowPersistence": {
///           "ConnectionString": "Server=localhost;Database=elsa;User=root;Password=secret"
///         }
///       },
///       "Features": ["Elsa", "MySqlWorkflowPersistence"]
///     }]
///   }
/// }
/// </code>
/// </example>
/// </remarks>
[ShellFeature(
    DisplayName = "MySql Workflow Persistence",
    Description = "Provides MySql persistence for workflow definitions, instances, and runtime data with unified configuration",
    DependsOn = [typeof(MySqlWorkflowDefinitionPersistenceShellFeature), typeof(MySqlWorkflowInstancePersistenceShellFeature), typeof(MySqlWorkflowRuntimePersistenceShellFeature)])]
[UsedImplicitly]
[ManifestInfrastructure("mysql-database", "database", Reason = "Stores workflow definitions, instances, and runtime data in MySQL.", Providers = new[] { "MySQL" }, ConfigurationKeys = new[] { "ConnectionString" })]
public class MySqlWorkflowPersistenceShellFeature : CombinedPersistenceShellFeatureBase
{
}
