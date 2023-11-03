using Elsa.EntityFrameworkCore.Common.Abstractions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Tenants.Accessors;
using Elsa.Tenants.Options;
using Elsa.Tenants.Providers;
using Elsa.Tenants.Strategies;
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

    /// <summary>
    /// A delegate that creates an instance of an implementation of <see cref="ITenantAccessor"/>.
    /// </summary>
    public Func<IServiceProvider, ITenantAccessor> TenantAccessor { get; set; } = sp => sp.GetRequiredService<TenantAccessor>();

    /// <inheritdoc />
    public override void Apply()
    {
        Services.Configure(TenantsOptions);

        Services
            .AddSingleton(TenantAccessor)
            .AddSingleton(TenantProvider)
        ;
    }

    /// <summary>
    /// Configures the feature to use <see cref="ConfigurationBasedUserProvider"/>.
    /// </summary>
    public TenantsFeature UseConfigurationBasedTenantProvider(Action<TenantsOptions>? configure = default)
    {
        Services
            .AddSingleton<TenantAccessor>()
            .AddSingleton<ConfigurationTenantProvider>();

        TenantAccessor = sp => sp.GetRequiredService<TenantAccessor>();
        TenantProvider = sp => sp.GetRequiredService<ConfigurationTenantProvider>();

        if (configure != null)
            TenantsOptions += configure;

        return this;
    }

    /// <summary>
    /// Configures the feature to use <see cref="ConfigurationBasedUserProvider"/>.
    /// </summary>
    public TenantsFeature UseExternalTenantProvider(Action<TenantsOptions>? configure = default)
    {
        Services.AddSingleton<ExternalTenantAccessor>();

        TenantAccessor = sp => sp.GetRequiredService<ExternalTenantAccessor>();

        if (configure != null)
            TenantsOptions += configure;

        return this;
    }

    public TenantsFeature UseEfcoreStrategies()
    {
        Services
            .AddSingleton<IDbContextStrategy, MustHaveTenantIdBeforeSavingStrategy>()
        ;

        return this;
    }
}