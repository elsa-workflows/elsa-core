using CShells.Features;
using Elsa.ReactStudio.Host;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ReactStudio.ActivityEditors;

/// <summary>Contributes the independently built ActivityEditors Preview module when enabled.</summary>
[ShellFeature("ReactStudio.ActivityEditors", DependsOn = [typeof(ReactStudioPreviewFeature)])]
public sealed class ActivityEditorsFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(new ReactStudioModule("activity-editors", "ActivityEditors", "Elsa.ReactStudio.ActivityEditors", true,
            typeof(ActivityEditorsFeature).Assembly, []));
    }
}
