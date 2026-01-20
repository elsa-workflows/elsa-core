using CShells.Features;
using Elsa.Persistence.EFCore.Modules.Management;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.Persistence.EFCore.Sqlite.ShellFeatures.Management;

/// <summary>
/// Configures the management feature to use Sqlite persistence.
/// </summary>
[ShellFeature(
    DisplayName = "Sqlite Workflow Definition Persistence",
    Description = "Provides Sqlite persistence for workflow definitions")]
[UsedImplicitly]
public class SqliteWorkflowDefinitionPersistenceShellFeature
    : DatabaseProviderShellFeature<EFCoreWorkflowDefinitionPersistenceShellFeature, ManagementElsaDbContext, SqliteDbContextOptionsBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteWorkflowDefinitionPersistenceShellFeature"/> class.
    /// </summary>
    public SqliteWorkflowDefinitionPersistenceShellFeature()
        : base(new SqliteProviderConfigurator(typeof(SqliteWorkflowDefinitionPersistenceShellFeature).Assembly))
    {
    }
}
