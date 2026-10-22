using CShells.Features;
using Elsa.ReactStudio.Host;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ReactStudio.WorkflowDefinitions;

/// <summary>Contributes the independently built WorkflowDefinitions Preview module when enabled.</summary>
[ShellFeature("ReactStudio.WorkflowDefinitions", DependsOn = [typeof(ReactStudioPreviewFeature), typeof(Elsa.ReactStudio.Designer.DesignerFeature), typeof(Elsa.ReactStudio.ActivityEditors.ActivityEditorsFeature)])]
public sealed class WorkflowDefinitionsFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(new ReactStudioModule("workflow-definitions", "WorkflowDefinitions", "Elsa.ReactStudio.WorkflowDefinitions", true,
            typeof(WorkflowDefinitionsFeature).Assembly, []));
    }
}
