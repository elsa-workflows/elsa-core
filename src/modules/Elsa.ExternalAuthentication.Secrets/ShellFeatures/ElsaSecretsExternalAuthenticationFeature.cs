using CShells.Features;
using Elsa.ExternalAuthentication.ShellFeatures;
using Elsa.Platform.PackageManifest.Generator.Hints;
using Elsa.Secrets.ShellFeatures;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ExternalAuthentication.Secrets.ShellFeatures;

[ManifestFeatureCategory("Identity")]
[ManifestFeatureCategory("Security")]
[ShellFeature(
    DisplayName = "Elsa Secrets External Authentication",
    Description = "Provides managed External Authentication secret bindings backed by Elsa Secrets.",
    DependsOn = [typeof(ExternalAuthenticationShellFeature), typeof(SecretsFeature)])]
[UsedImplicitly]
public sealed class ElsaSecretsExternalAuthenticationFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddElsaSecretsExternalAuthentication();
    }
}
