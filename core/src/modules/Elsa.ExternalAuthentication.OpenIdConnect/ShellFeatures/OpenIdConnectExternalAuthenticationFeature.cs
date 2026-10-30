using CShells.Features;
using Elsa.ExternalAuthentication.ShellFeatures;
using Elsa.Platform.PackageManifest.Generator.Hints;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ExternalAuthentication.OpenIdConnect.ShellFeatures;

[ManifestFeatureCategory("Identity")]
[ManifestFeatureCategory("Security")]
[ShellFeature(
    DisplayName = "OpenID Connect External Authentication",
    Description = "Provides the OpenID Connect protocol adapter for External Authentication.",
    DependsOn = [typeof(ExternalAuthenticationShellFeature)])]
[UsedImplicitly]
public sealed class OpenIdConnectExternalAuthenticationFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddOpenIdConnectExternalAuthentication();
    }
}
