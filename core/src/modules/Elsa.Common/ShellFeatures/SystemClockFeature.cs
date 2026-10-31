using CShells.Features;
using Elsa.Common.Services;
using Elsa.Platform.PackageManifest.Generator.Hints;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Common.ShellFeatures;

/// <summary>
/// Configures the system clock.
/// </summary>
[ManifestFeatureCategory("Infrastructure")]
[ShellFeature(
    "SystemClock",
    DisplayName = "System Clock",
    Description = "Provides the default system clock service")]
public class SystemClockFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ISystemClock, SystemClock>();
    }
}
