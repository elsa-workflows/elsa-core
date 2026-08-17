using CShells.Features;
using Elsa.Platform.Integration.Options;
using Elsa.Platform.Integration.Services;
using Elsa.Platform.PackageManifest.Generator.Hints;
using Elsa.Workflows.Management.ShellFeatures;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Platform.Integration.ShellFeatures;

[ManifestFeatureCategory("Integrations")]
[ShellFeature(
    DisplayName = "Elsa Platform Integration",
    Description = "Polls Elsa Platform deployment commands and applies Loom recipe artifacts to this Elsa runtime",
    DependsOn = [typeof(WorkflowManagementFeature)])]
[UsedImplicitly]
public class ElsaPlatformIntegrationFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddOptions<ElsaPlatformIntegrationOptions>()
            .BindConfiguration(ElsaPlatformIntegrationOptions.ConfigurationSection);

        services.AddHttpClient<IPlatformRuntimeCommandClient, PlatformRuntimeCommandClient>();
        services.AddSingleton<IShellConfigurationOverlayStore, FileShellConfigurationOverlayStore>();
        services.AddScoped<IPlatformRecipeArtifactApplier, ElsaLoomRecipeArtifactApplier>();
        services.AddHostedService<ElsaPlatformDeploymentWorker>();
    }
}
