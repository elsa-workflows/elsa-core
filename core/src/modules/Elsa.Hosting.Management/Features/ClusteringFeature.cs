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
public class ClusteringFeature : FeatureBase
{
    /// <inheritdoc />
    public ClusteringFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// A factory that instantiates an <see cref="IApplicationInstanceNameProvider"/>.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="ConfiguredApplicationInstanceNameProvider"/>, which honors
    /// <see cref="ApplicationInstanceOptions"/> for a stable instance name and falls back to a random
    /// name when none is configured (preserving the previous default behaviour).
    /// </remarks>
    public Func<IServiceProvider, IApplicationInstanceNameProvider> InstanceNameProvider { get; set; } = sp =>
    {
        return ActivatorUtilities.CreateInstance<ConfiguredApplicationInstanceNameProvider>(sp);
    };

    /// <summary>
    /// Represents the options for heartbeat feature.
    /// </summary>
    public Action<HeartbeatOptions> HeartbeatOptions { get; set; } = _ => { };

    /// <summary>
    /// Configures how the application instance name is determined. Set a stable name (for example from
    /// the pod name) to avoid accumulating orphaned per-instance transport entities across restarts.
    /// </summary>
    public Action<ApplicationInstanceOptions> ApplicationInstanceOptions { get; set; } = _ => { };

    /// <inheritdoc />
    public override void ConfigureHostedServices()
    {
        Module.ConfigureHostedService<InstanceHeartbeatService>();
        Module.ConfigureHostedService<InstanceHeartbeatMonitorService>();
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.Configure(HeartbeatOptions)
            .Configure(ApplicationInstanceOptions)
            .AddSingleton(InstanceNameProvider)
            .AddSingleton<RandomIntIdentityGenerator>();
    }
}