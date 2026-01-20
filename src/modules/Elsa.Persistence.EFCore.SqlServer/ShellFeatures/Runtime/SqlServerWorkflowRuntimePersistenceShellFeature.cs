using CShells.Features;
using Elsa.Persistence.EFCore.Modules.Runtime;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.Persistence.EFCore.SqlServer.ShellFeatures.Runtime;

/// <summary>
/// Configures the runtime feature to use SqlServer persistence.
/// </summary>
[ShellFeature(
    DisplayName = "SqlServer Workflow Runtime Persistence",
    Description = "Provides SqlServer persistence for workflow runtime")]
[UsedImplicitly]
public class SqlServerWorkflowRuntimePersistenceShellFeature
    : DatabaseProviderShellFeature<EFCoreWorkflowRuntimePersistenceShellFeature, RuntimeElsaDbContext, SqlServerDbContextOptionsBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerWorkflowRuntimePersistenceShellFeature"/> class.
    /// </summary>
    public SqlServerWorkflowRuntimePersistenceShellFeature()
        : base(new SqlServerProviderConfigurator(typeof(SqlServerWorkflowRuntimePersistenceShellFeature).Assembly))
    {
    }
}
