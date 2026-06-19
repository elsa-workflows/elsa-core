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

    public void ConfigureServices(IServiceCollection services)
    {
        services.Configure(HeartbeatOptions)
            .Configure(ApplicationInstanceOptions)
            .AddSingleton(InstanceNameProvider)
            .AddSingleton<RandomIntIdentityGenerator>()
            .AddHostedService<InstanceHeartbeatService>()
            .AddHostedService<InstanceHeartbeatMonitorService>();
    }
}
