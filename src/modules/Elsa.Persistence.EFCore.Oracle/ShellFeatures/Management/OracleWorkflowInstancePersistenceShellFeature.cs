using CShells.Features;
using Elsa.Persistence.EFCore.Modules.Management;
using JetBrains.Annotations;
using Oracle.EntityFrameworkCore.Infrastructure;

namespace Elsa.Persistence.EFCore.Oracle.ShellFeatures.Management;

/// <summary>
/// Configures the management feature to use Oracle persistence.
/// </summary>
[ShellFeature(
    DisplayName = "Oracle Workflow Instance Persistence",
    Description = "Provides Oracle persistence for workflow instances")]
[UsedImplicitly]
public class OracleWorkflowInstancePersistenceShellFeature
    : DatabaseProviderShellFeature<EFCoreWorkflowInstancePersistenceShellFeature, ManagementElsaDbContext, OracleDbContextOptionsBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OracleWorkflowInstancePersistenceShellFeature"/> class.
    /// </summary>
    public OracleWorkflowInstancePersistenceShellFeature()
        : base(new OracleProviderConfigurator(typeof(OracleWorkflowInstancePersistenceShellFeature).Assembly))
    {
    }
}
