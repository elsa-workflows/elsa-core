using CShells.Features;
using Elsa.Workflows.Management.Stores;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Management.ShellFeatures;

/// <summary>
/// Configures workflow definition storage.
/// </summary>
[ShellFeature(
    DisplayName = "Workflow Definitions",
    Description = "Manages workflow definitions and their storage")]
[UsedImplicitly]
public class WorkflowDefinitionsFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IWorkflowDefinitionStore, MemoryWorkflowDefinitionStore>();
    }
}
