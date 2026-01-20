using CShells.Features;
using Elsa.Persistence.EFCore.Modules.Runtime;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.Persistence.EFCore.Sqlite.ShellFeatures.Runtime;

/// <summary>
/// Configures the runtime feature to use Sqlite persistence.
/// </summary>
[ShellFeature(
    DisplayName = "Sqlite Workflow Runtime Persistence",
    Description = "Provides Sqlite persistence for workflow runtime")]
[UsedImplicitly]
public class SqliteWorkflowRuntimePersistenceShellFeature
    : DatabaseProviderShellFeature<EFCoreWorkflowRuntimePersistenceShellFeature, RuntimeElsaDbContext, SqliteDbContextOptionsBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteWorkflowRuntimePersistenceShellFeature"/> class.
    /// </summary>
    public SqliteWorkflowRuntimePersistenceShellFeature()
        : base(new SqliteProviderConfigurator(typeof(SqliteWorkflowRuntimePersistenceShellFeature).Assembly))
    {
    }
}
