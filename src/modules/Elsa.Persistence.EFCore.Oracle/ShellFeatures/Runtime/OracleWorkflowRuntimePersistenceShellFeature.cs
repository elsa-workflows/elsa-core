using CShells.Features;
using Elsa.Persistence.EFCore.Modules.Runtime;
using JetBrains.Annotations;
using Oracle.EntityFrameworkCore.Infrastructure;

namespace Elsa.Persistence.EFCore.Oracle.ShellFeatures.Runtime;

/// <summary>
/// Configures the runtime feature to use Oracle persistence.
/// </summary>
[ShellFeature(
    DisplayName = "Oracle Workflow Runtime Persistence",
    Description = "Provides Oracle persistence for workflow runtime")]
[UsedImplicitly]
public class OracleWorkflowRuntimePersistenceShellFeature
    : DatabaseProviderShellFeature<EFCoreWorkflowRuntimePersistenceShellFeature, RuntimeElsaDbContext, OracleDbContextOptionsBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OracleWorkflowRuntimePersistenceShellFeature"/> class.
    /// </summary>
    public OracleWorkflowRuntimePersistenceShellFeature()
        : base(new OracleProviderConfigurator(typeof(OracleWorkflowRuntimePersistenceShellFeature).Assembly))
    {
    }
}
