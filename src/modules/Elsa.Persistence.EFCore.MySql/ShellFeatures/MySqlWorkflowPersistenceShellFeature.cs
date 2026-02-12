using CShells.Features;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore.MySql.ShellFeatures;

/// <summary>
/// Configures the persistence feature to use MySql persistence.
/// </summary>
[ShellFeature(
    DisplayName = "MySql Workflow Persistence",
    Description = "Provides MySql persistence for workflow definitions and runtime data",
    DependsOn = ["MySqlWorkflowDefinitionPersistence", "MySqlWorkflowInstancePersistence", "MySqlWorkflowRuntimePersistence"])]
[UsedImplicitly]
public class MySqlWorkflowPersistenceShellFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
    }
}
