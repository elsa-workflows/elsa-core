using CShells.Features;
using Elsa.Persistence.EFCore;
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
    DependsOn = ["MySqlWorkflowDefinitionPersistence", "MySqlWorkflowInstancePersistence", "MySqlWorkflowRuntimePersistence"])]
[UsedImplicitly]
public class MySqlWorkflowPersistenceShellFeature : CombinedPersistenceShellFeatureBase
{
}
