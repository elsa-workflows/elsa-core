using CShells.FastEndpoints.Features;
using CShells.Features;
using Elsa.ShellFeatures;
using Elsa.Platform.PackageManifest.Generator.Hints;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Shells.Api.ShellFeatures;

/// <summary>
/// Adds shells API features with FastEndpoints support.
/// </summary>
/// <remarks>
/// This feature implements <see cref="IFastEndpointsShellFeature"/> to indicate that this assembly
/// contains FastEndpoints that should be automatically discovered and registered.
/// </remarks>
[ManifestFeatureCategory("Shells")]
[ManifestFeatureCategory("API")]
[ShellFeature(
    DisplayName = "Shells API",
    Description = "Provides REST API endpoints for shell management",
    DependsOn = [typeof(ElsaFastEndpointsFeature)])]
[UsedImplicitly]
public class ShellsApiFeature : IFastEndpointsShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
    }
}