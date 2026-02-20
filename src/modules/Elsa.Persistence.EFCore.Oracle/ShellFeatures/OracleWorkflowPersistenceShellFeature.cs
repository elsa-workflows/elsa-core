using CShells.Features;
using Elsa.Persistence.EFCore;
using JetBrains.Annotations;

namespace Elsa.Persistence.EFCore.Oracle.ShellFeatures;

/// <summary>
/// Configures the persistence feature to use Oracle persistence for all workflow data.
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
///         "OracleWorkflowPersistence": {
///           "ConnectionString": "User Id=elsa;Password=secret;Data Source=//localhost:1521/ORCLPDB1"
///         }
///       },
///       "Features": ["Elsa", "OracleWorkflowPersistence"]
///     }]
///   }
/// }
/// </code>
/// </example>
/// </remarks>
[ShellFeature(
    DisplayName = "Oracle Workflow Persistence",
    Description = "Provides Oracle persistence for workflow definitions, instances, and runtime data with unified configuration",
    DependsOn = ["OracleWorkflowDefinitionPersistence", "OracleWorkflowInstancePersistence", "OracleWorkflowRuntimePersistence"])]
[UsedImplicitly]
public class OracleWorkflowPersistenceShellFeature : CombinedPersistenceShellFeatureBase
{
}
