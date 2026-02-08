using CShells.Features;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore.Sqlite.ShellFeatures;

/// <summary>
/// Configures the persistence feature to use Sqlite persistence.
/// </summary>
[ShellFeature(
    DisplayName = "Sqlite Workflow Persistence",
    Description = "Provides Sqlite persistence for workflow definitions and runtime data",
    DependsOn = ["SqliteWorkflowDefinitionPersistence", "SqliteWorkflowInstancePersistence", "SqliteWorkflowRuntimePersistence"])]
[UsedImplicitly]
public class SqliteWorkflowPersistenceShellFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
    }
}