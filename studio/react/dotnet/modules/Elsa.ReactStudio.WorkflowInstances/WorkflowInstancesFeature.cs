using CShells.Features;
using Elsa.ReactStudio.Host;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ReactStudio.WorkflowInstances;

/// <summary>Contributes the independently built WorkflowInstances Preview module when enabled.</summary>
[ShellFeature("ReactStudio.WorkflowInstances", DependsOn = [typeof(ReactStudioPreviewFeature), typeof(Elsa.ReactStudio.Designer.DesignerFeature), typeof(Elsa.ReactStudio.WorkflowDefinitions.WorkflowDefinitionsFeature)])]
public sealed class WorkflowInstancesFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(new ReactStudioModule("workflow-instances", "WorkflowInstances", "Elsa.ReactStudio.WorkflowInstances", true,
            typeof(WorkflowInstancesFeature).Assembly, []));
    }
}
