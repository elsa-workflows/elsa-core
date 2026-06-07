using CShells.Features;
using Elsa.Common.ShellHandlers;
using Elsa.Extensions;
using Elsa.Platform.PackageManifest.Generator.Hints;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Common.ShellFeatures;

/// <summary>
/// Adds and configures the Mediator feature.
/// </summary>
[ManifestFeatureCategory(ManifestFeatureCategories.Infrastructure)]
[ShellFeature(
    "Mediator",
    DisplayName = "Mediator",
    Description = "Registers mediator services for in-process notifications and requests",
    DependsOn = [typeof(MultitenancyFeature)])]
public class MediatorFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddMediator()
            .AddMediatorBackgroundChannels();

        services.TryAddSingleton<MediatorBackgroundProcessingCoordinator>();
        services.AddBackgroundTask<MediatorBackgroundTask>();
    }
}
