using Elsa.Common.Contracts;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Identity.Providers;
using Elsa.Tenants.Accessors;
using Elsa.Tenants.Contracts;
using Elsa.Tenants.Handlers;
using Elsa.Tenants.Middlewares;
using Elsa.Tenants.Options;
using Elsa.Tenants.Providers;
using Elsa.Tenants.Services;
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
    /// A delegate that creates an instance of an implementation of <see cref="ITenantsProvider"/>.
    /// </summary>
    public Func<IServiceProvider, ITenantsProvider> TenantsProvider { get; set; } = sp => sp.GetRequiredService<ConfigurationTenantsProvider>();

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
            .AddScoped<ITenantAccessor, TenantAccessor>()
            .AddScoped<ITenantServiceScopeFactory, TenantServiceScopeFactory>()
            .AddSingleton<ConfigurationTenantsProvider>()
            .AddScoped(TenantsProvider)

            .RemoveAll<DispatchWorkflowRequestHandler>()
            .AddScoped<DispatchTenantWorkflowRequestHandler>()
        ;
    }

    /// <summary>
    /// Configures the feature to use <see cref="ConfigurationBasedUserProvider"/>.
    /// </summary>
    public TenantsFeature UseConfigurationBasedTenantsProvider()
    {
        TenantsProvider = sp => sp.GetRequiredService<ConfigurationTenantsProvider>();
        return this;
    }

    /// <summary>
    /// Configures the feature to use <see cref="ConfigurationBasedUserProvider"/>.
    /// </summary>
    public TenantsFeature UseExternalTenantResolverMiddleware()
    {
        
        return this;
    }
}
