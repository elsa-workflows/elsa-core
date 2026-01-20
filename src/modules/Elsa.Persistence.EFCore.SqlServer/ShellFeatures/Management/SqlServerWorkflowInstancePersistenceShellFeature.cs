using CShells.Features;
using Elsa.Persistence.EFCore.Modules.Management;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.Persistence.EFCore.SqlServer.ShellFeatures.Management;

/// <summary>
/// Configures the management feature to use SqlServer persistence.
/// </summary>
[ShellFeature(
    DisplayName = "SqlServer Workflow Instance Persistence",
    Description = "Provides SqlServer persistence for workflow instances")]
[UsedImplicitly]
public class SqlServerWorkflowInstancePersistenceShellFeature
    : DatabaseProviderShellFeature<EFCoreWorkflowInstancePersistenceShellFeature, ManagementElsaDbContext, SqlServerDbContextOptionsBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerWorkflowInstancePersistenceShellFeature"/> class.
    /// </summary>
    public SqlServerWorkflowInstancePersistenceShellFeature()
        : base(new SqlServerProviderConfigurator(typeof(SqlServerWorkflowInstancePersistenceShellFeature).Assembly))
    {
    }
}
