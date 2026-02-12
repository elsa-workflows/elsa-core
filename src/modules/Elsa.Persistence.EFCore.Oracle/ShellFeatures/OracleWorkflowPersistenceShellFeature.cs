using CShells.Features;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore.Oracle.ShellFeatures;

/// <summary>
/// Configures the persistence feature to use Oracle persistence.
/// </summary>
[ShellFeature(
    DisplayName = "Oracle Workflow Persistence",
    Description = "Provides Oracle persistence for workflow definitions and runtime data",
    DependsOn = ["OracleWorkflowDefinitionPersistence", "OracleWorkflowInstancePersistence", "OracleWorkflowRuntimePersistence"])]
[UsedImplicitly]
public class OracleWorkflowPersistenceShellFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
    }
}
