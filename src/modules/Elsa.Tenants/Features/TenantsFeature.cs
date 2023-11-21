using Elsa.Common.Contracts;
using Elsa.EntityFrameworkCore.Common.Abstractions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Tenants.Accessors;
using Elsa.Tenants.Handlers;
using Elsa.Tenants.Options;
using Elsa.Tenants.Providers;
using Elsa.Tenants.Services;
using Elsa.Tenants.Strategies;
using Elsa.Workflows.Runtime.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Tenants.Features;

/// <summary>
/// Configures liquid functionality.
/// </summary>
[DependsOn(typeof(WorkflowRuntimeFeature))]
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
    public override void Configure()
    {

        Module.Configure<WorkflowRuntimeFeature>(feature =>
        {
            feature.WorkflowDispatcher = sp => sp.GetRequiredService<BackgroundTenantWorkflowDispatcher>();
            feature.DispatchWorkflowCommandHandler = sp => sp.GetRequiredService<DispatchTenantWorkflowRequestHandler>();
        });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.Configure(TenantsOptions);

        Services
            .AddScoped<BackgroundTenantWorkflowDispatcher>()
            .AddScoped<DispatchTenantWorkflowRequestHandler>()
            .AddScoped(TenantAccessor)
            .AddScoped(TenantProvider)
        ;
    }

    /// <summary>
    /// Configures the feature to use <see cref="ConfigurationBasedUserProvider"/>.
    /// </summary>
    public TenantsFeature UseConfigurationBasedTenantProvider(Action<TenantsOptions>? configure = default)
    {
        Services
            .AddScoped<TenantAccessor>()
            .AddScoped<ConfigurationTenantProvider>();

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
        Services
            .AddScoped<ExternalTenantAccessor>();

        TenantAccessor = sp => sp.GetRequiredService<ExternalTenantAccessor>();

        if (configure != null)
            TenantsOptions += configure;

        return this;
    }

    public TenantsFeature UseEfcoreStrategies()
    {
        Services
            .AddScoped<IDbContextStrategy, MustHaveTenantIdBeforeSavingStrategy>()
        ;

        return this;
    }
}