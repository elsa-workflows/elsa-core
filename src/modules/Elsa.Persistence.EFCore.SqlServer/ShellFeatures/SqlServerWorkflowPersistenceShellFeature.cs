using CShells.Features;
using Elsa.Persistence.EFCore;
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
    DependsOn = ["SqlServerWorkflowDefinitionPersistence", "SqlServerWorkflowInstancePersistence", "SqlServerWorkflowRuntimePersistence"])]
[UsedImplicitly]
public class SqlServerWorkflowPersistenceShellFeature : CombinedPersistenceShellFeatureBase
{
}

