using CShells.Features;
using Elsa.Persistence.EFCore.Modules.Management;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.Persistence.EFCore.Sqlite.ShellFeatures.Management;

/// <summary>
/// Configures the management feature to use Sqlite persistence.
/// </summary>
[ShellFeature(
    DisplayName = "Sqlite Workflow Instance Persistence",
    Description = "Provides Sqlite persistence for workflow instances")]
[UsedImplicitly]
public class SqliteWorkflowInstancePersistenceShellFeature
    : DatabaseProviderShellFeature<EFCoreWorkflowInstancePersistenceShellFeature, ManagementElsaDbContext, SqliteDbContextOptionsBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteWorkflowInstancePersistenceShellFeature"/> class.
    /// </summary>
    public SqliteWorkflowInstancePersistenceShellFeature()
        : base(new SqliteProviderConfigurator(typeof(SqliteWorkflowInstancePersistenceShellFeature).Assembly))
    {
    }
}
