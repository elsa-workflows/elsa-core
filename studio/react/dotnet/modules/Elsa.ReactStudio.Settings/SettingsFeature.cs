using CShells.Features;
using Elsa.ReactStudio.Host;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ReactStudio.Settings;

/// <summary>Contributes the independently built Settings Preview module when enabled.</summary>
[ShellFeature("ReactStudio.Settings", DependsOn = [typeof(ReactStudioPreviewFeature)])]
public sealed class SettingsFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(new ReactStudioModule("settings", "Settings", "Elsa.ReactStudio.Settings", false,
            typeof(SettingsFeature).Assembly, []));
    }
}
