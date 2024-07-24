using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Hosting.Management.Contracts;
using Elsa.Hosting.Management.HostedServices;
using Elsa.Hosting.Management.Options;
using Elsa.Hosting.Management.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Hosting.Management.Features;

/// <summary>
/// Installs and configures the clustering feature.
/// </summary>
/// <inheritdoc />
public class ClusteringFeature(IModule module) : FeatureBase(module)
{

    /// <summary>
    /// A factory that instantiates an <see cref="IApplicationInstanceNameProvider"/>.
    /// </summary>
    public Func<IServiceProvider, IApplicationInstanceNameProvider> InstanceNameProvider { get; set; } = sp =>
    {
        return ActivatorUtilities.CreateInstance<RandomApplicationInstanceNameProvider>(sp);
    };

    /// <summary>
    /// Represents the options for heartbeat feature.
    /// </summary>
    public Action<HeartbeatOptions> HeartbeatOptions { get; set; } = _ => { };

    /// <inheritdoc />
    public override void ConfigureHostedServices()
    {
        Module.ConfigureHostedService<InstanceHeartbeatService>();
        Module.ConfigureHostedService<InstanceHeartbeatMonitorService>();
    }

    /// <inheritdoc />
    public override void Apply() => 
        Services.Configure(HeartbeatOptions)
            .AddSingleton(InstanceNameProvider)
            .AddSingleton<RandomIntIdentityGenerator>();
}