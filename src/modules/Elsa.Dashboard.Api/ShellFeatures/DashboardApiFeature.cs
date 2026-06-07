using CShells.FastEndpoints.Features;
using CShells.Features;
using Elsa.Dashboard.Api.Extensions;
using Elsa.ShellFeatures;
using Elsa.Platform.PackageManifest.Generator.Hints;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Dashboard.Api.ShellFeatures;

[ManifestFeatureCategory(ManifestFeatureCategories.Dashboard)]
[ManifestFeatureCategory(ManifestFeatureCategories.API)]
[ShellFeature(
    DisplayName = "Dashboard API",
    Description = "Provides operational dashboard API endpoints for Elsa Studio",
    DependsOn = [typeof(ElsaFastEndpointsFeature)])]
[UsedImplicitly]
public class DashboardApiFeature : IFastEndpointsShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddDashboardApiServices();
    }
}
