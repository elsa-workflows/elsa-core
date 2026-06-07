using CShells.Features;
using Elsa.Persistence.EFCore.Oracle.ShellFeatures.Management;
using Elsa.Persistence.EFCore.Oracle.ShellFeatures.Runtime;
using Elsa.Persistence.EFCore;
using Elsa.Platform.PackageManifest.Generator.Hints;
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
[ManifestFeatureCategory(ManifestFeatureCategories.Persistence)]
[ManifestFeatureCategory(ManifestFeatureCategories.Workflows)]
[ShellFeature(
    DisplayName = "Oracle Workflow Persistence",
    Description = "Provides Oracle persistence for workflow definitions, instances, and runtime data with unified configuration",
    DependsOn = [typeof(OracleWorkflowDefinitionPersistenceShellFeature), typeof(OracleWorkflowInstancePersistenceShellFeature), typeof(OracleWorkflowRuntimePersistenceShellFeature)])]
[UsedImplicitly]
[ManifestInfrastructure("oracle-database", "database", Reason = "Stores workflow definitions, instances, and runtime data in Oracle Database.", Providers = new[] { "Oracle" }, ConfigurationKeys = new[] { "ConnectionString" })]
public class OracleWorkflowPersistenceShellFeature : CombinedPersistenceShellFeatureBase
{
}
