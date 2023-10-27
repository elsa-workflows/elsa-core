using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Tenants.Accessors;
using Elsa.Tenants.Options;
using Elsa.Tenants.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Tenants.Features;

/// <summary>
/// Configures liquid functionality.
/// </summary>
public class TenantsFeature : FeatureBase
{
    /// <inheritdoc />
    public TenantsFeature(IModule serviceConfiguration) : base(serviceConfiguration)
    {
    }

    /// <summary>
    /// Configures the Tenants options.
    /// </summary>
    public Action<TenantsOptions> TenantsOptions { get; set; } = _ => { };

    /// <summary>
    /// A delegate that creates an instance of an implementation of <see cref="ITenantProvider"/>.
    /// </summary>
    public Func<IServiceProvider, ITenantProvider> TenantProvider { get; set; } = sp => sp.GetRequiredService<ConfigurationTenantProvider>();

    /// <inheritdoc />
    public override void Apply()
    {
        Services.Configure(TenantsOptions);

        Services
            .AddSingleton<ConfigurationTenantProvider>()
            .AddSingleton<ITenantAccessor, TenantAccessor>()
            .AddSingleton(TenantProvider)
        ;
    }
    /// <summary>
    /// Configures the feature to use <see cref="ConfigurationBasedUserProvider"/>.
    /// </summary>
    public void UseConfigurationBasedTenantProvider(Action<TenantsOptions>? configure = default)
    {
        TenantProvider = sp => sp.GetRequiredService<ConfigurationTenantProvider>();

        if (configure != null)
            TenantsOptions += configure;
    }
}