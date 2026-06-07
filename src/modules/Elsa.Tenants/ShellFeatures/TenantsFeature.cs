using CShells.Features;
using Elsa.Common.Multitenancy;
using Elsa.Tenants.Mediator.Tasks;
using Elsa.Tenants.Options;
using Elsa.Platform.PackageManifest.Generator.Hints;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Tenants.ShellFeatures;

/// <summary>
/// Configures multi-tenancy features.
/// </summary>
[ManifestFeatureCategory(ManifestFeatureCategories.Tenancy)]
[ShellFeature(
    DisplayName = "Tenants",
    Description = "Provides multi-tenancy capabilities for workflows")]
[UsedImplicitly]
public class TenantsFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddHostedService<SetupMediatorPipelines>()
            .AddScoped<ITenantResolverPipelineInvoker, DefaultTenantResolverPipelineInvoker>()
            .AddScoped<ITenantResolver, DefaultTenantResolver>();
    }
}

