using CShells.Features;
using Elsa.Persistence.EFCore.Modules.Management;
using JetBrains.Annotations;
using Oracle.EntityFrameworkCore.Infrastructure;

namespace Elsa.Persistence.EFCore.Oracle.ShellFeatures.Management;

/// <summary>
/// Configures the management feature to use Oracle persistence.
/// </summary>
[ShellFeature(
    DisplayName = "Oracle Workflow Definition Persistence",
    Description = "Provides Oracle persistence for workflow definitions")]
[UsedImplicitly]
public class OracleWorkflowDefinitionPersistenceShellFeature
    : DatabaseProviderShellFeature<EFCoreWorkflowDefinitionPersistenceShellFeature, ManagementElsaDbContext, OracleDbContextOptionsBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OracleWorkflowDefinitionPersistenceShellFeature"/> class.
    /// </summary>
    public OracleWorkflowDefinitionPersistenceShellFeature()
        : base(new OracleProviderConfigurator(typeof(OracleWorkflowDefinitionPersistenceShellFeature).Assembly))
    {
    }
}
