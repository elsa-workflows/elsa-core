using CShells.Features;
using Elsa.Hosting.Management.Contracts;
using Elsa.Hosting.Management.HostedServices;
using Elsa.Hosting.Management.Options;
using Elsa.Hosting.Management.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Hosting.Management.ShellFeatures;

/// <summary>
/// Installs and configures the clustering feature.
/// </summary>
[ShellFeature(
    DisplayName = "Clustering",
    Description = "Provides clustering and heartbeat capabilities for distributed Elsa deployments")]
[UsedImplicitly]
public class ClusteringFeature : IShellFeature
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

    public void ConfigureServices(IServiceCollection services)
    {
        services.Configure(HeartbeatOptions)
            .AddSingleton(InstanceNameProvider)
            .AddSingleton<RandomIntIdentityGenerator>()
            .AddHostedService<InstanceHeartbeatService>()
            .AddHostedService<InstanceHeartbeatMonitorService>();
    }
}

