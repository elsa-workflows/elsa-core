using CShells.Features;
using Elsa.ReactStudio.Host;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ReactStudio.Labels;

/// <summary>Contributes the independently built Labels Preview module when enabled.</summary>
[ShellFeature("ReactStudio.Labels", DependsOn = [typeof(ReactStudioPreviewFeature)])]
public sealed class LabelsFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(new ReactStudioModule("labels", "Labels", "Elsa.ReactStudio.Labels", false,
            typeof(LabelsFeature).Assembly, ["Elsa.Labels"]));
    }
}
