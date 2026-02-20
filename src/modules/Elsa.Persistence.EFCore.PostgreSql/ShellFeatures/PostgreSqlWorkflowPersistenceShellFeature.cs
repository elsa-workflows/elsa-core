using CShells.Features;
using Elsa.Persistence.EFCore;
using JetBrains.Annotations;

namespace Elsa.Persistence.EFCore.PostgreSql.ShellFeatures;

/// <summary>
/// Configures the persistence feature to use PostgreSQL persistence for all workflow data.
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
///         "PostgreSqlWorkflowPersistence": {
///           "ConnectionString": "Host=localhost;Database=elsa;Username=postgres;Password=secret"
///         }
///       },
///       "Features": ["Elsa", "PostgreSqlWorkflowPersistence"]
///     }]
///   }
/// }
/// </code>
/// </example>
/// </remarks>
[ShellFeature(
    DisplayName = "PostgreSql Workflow Persistence",
    Description = "Provides PostgreSQL persistence for workflow definitions, instances, and runtime data with unified configuration",
    DependsOn = ["PostgreSqlWorkflowDefinitionPersistence", "PostgreSqlWorkflowInstancePersistence", "PostgreSqlWorkflowRuntimePersistence"])]
[UsedImplicitly]
public class PostgreSqlWorkflowPersistenceShellFeature : CombinedPersistenceShellFeatureBase
{
}
