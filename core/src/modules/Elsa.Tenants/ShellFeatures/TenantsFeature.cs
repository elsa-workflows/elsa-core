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
[ManifestFeatureCategory("Tenancy")]
[ShellFeature(
    DisplayName = "Tenants",
    Description = "Provides multi-tenancy capabilities for workflows")]
[UsedImplicitly]
public class TenantsFeature : IShellFeature
{
    /// <summary>
    /// Gets or sets a value indicating whether a request, job or command whose tenant cannot be resolved is rejected instead of running as the default tenant.
    /// </summary>
    [ManifestSetting(
        DisplayName = "Reject Unresolved Tenants",
        Description = "Reject requests, jobs and commands whose tenant cannot be resolved instead of running them as the default tenant.",
        Category = "Tenancy",
        DefaultValue = "false",
        RestartRequired = true)]
    public bool RejectUnresolvedTenants { get; set; }

    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddHostedService<SetupMediatorPipelines>()
            .AddScoped<ITenantResolverPipelineInvoker, DefaultTenantResolverPipelineInvoker>()
            .AddScoped<ITenantResolver, DefaultTenantResolver>();

        if (RejectUnresolvedTenants)
        {
            services.Configure<MultitenancyOptions>(options => options.RejectUnresolvedTenants = true);
        }
    }
}

