using CShells.Features;
using Elsa.ReactStudio.Host;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ReactStudio.Appearance;

/// <summary>Contributes the independently built Appearance Preview module when enabled.</summary>
[ShellFeature("ReactStudio.Appearance", DependsOn = [typeof(ReactStudioPreviewFeature), typeof(Elsa.ReactStudio.Settings.SettingsFeature)])]
public sealed class AppearanceFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(new ReactStudioModule("appearance", "Appearance", "Elsa.ReactStudio.Appearance", false,
            typeof(AppearanceFeature).Assembly, []));
    }
}
