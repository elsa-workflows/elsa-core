using CShells.Features;
using Elsa.ReactStudio.Host;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ReactStudio.Designer;

/// <summary>Contributes the independently built Designer Preview module when enabled.</summary>
[ShellFeature("ReactStudio.Designer", DependsOn = [typeof(ReactStudioPreviewFeature), typeof(Elsa.ReactStudio.ActivityEditors.ActivityEditorsFeature)])]
public sealed class DesignerFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(new ReactStudioModule("designer", "Designer", "Elsa.ReactStudio.Designer", true,
            typeof(DesignerFeature).Assembly, []));
    }
}
