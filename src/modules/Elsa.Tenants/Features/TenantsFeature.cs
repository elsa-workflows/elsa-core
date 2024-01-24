using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Tenants.Handlers;
using Elsa.Tenants.Middlewares;
using Elsa.Tenants.Options;
using Elsa.Tenants.Providers;
using Elsa.Workflows.Runtime.Features;
using Elsa.Workflows.Runtime.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<WorkflowRuntimeFeature>(feature =>
        {
            feature.DispatchWorkflowCommandHandler = sp => sp.GetRequiredService<DispatchTenantWorkflowRequestHandler>();
        });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.Configure(TenantsOptions);

        Services
            .AddScoped<ConfigurationTenantProvider>()
            .AddScoped(TenantProvider)

            .AddScoped<HttpTenantMiddleware>()
            .AddScoped<HttpExternalTenantMiddleware>()

            .RemoveAll<DispatchWorkflowRequestHandler>()
            .AddScoped<DispatchTenantWorkflowRequestHandler>()
        ;
    }

    /// <summary>
    /// Configures the feature to use <see cref="ConfigurationBasedUserProvider"/>.
    /// </summary>
    public TenantsFeature UseConfigurationBasedTenantProvider(Action<TenantsOptions>? configure = default)
    {
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
            .AddScoped<HttpExternalTenantMiddleware>();

        if (configure != null)
            TenantsOptions += configure;

        return this;
    }
}
