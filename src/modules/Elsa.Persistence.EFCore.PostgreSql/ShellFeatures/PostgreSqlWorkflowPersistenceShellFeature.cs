using CShells.Features;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore.PostgreSql.ShellFeatures;

/// <summary>
/// Configures the persistence feature to use PostgreSql persistence.
/// </summary>
[ShellFeature(
    DisplayName = "PostgreSql Workflow Persistence",
    Description = "Provides PostgreSql persistence for workflow definitions and runtime data",
    DependsOn = ["PostgreSqlWorkflowDefinitionPersistence", "PostgreSqlWorkflowInstancePersistence", "PostgreSqlWorkflowRuntimePersistence"])]
[UsedImplicitly]
public class PostgreSqlWorkflowPersistenceShellFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
    }
}
