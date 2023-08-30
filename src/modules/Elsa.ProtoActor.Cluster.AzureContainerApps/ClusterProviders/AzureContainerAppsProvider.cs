using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Proto.Cluster.AzureContainerApps.Actors;
using Proto.Cluster.AzureContainerApps.Messages;
using Proto.DependencyInjection;

namespace Proto.Cluster.AzureContainerApps.ClusterProviders;

/// <summary>
/// A cluster provider that uses Azure Container Apps to host the cluster.
/// </summary>
[PublicAPI]
public class AzureContainerAppsProvider : IClusterProvider
{
    private readonly ILogger _logger;
    
    private string _address = default!;
    private Cluster _cluster = default!;
    private string _clusterName = default!;
    private string[] _kinds = default!;
    private string _host = default!;
    private int _port;
    private PID _clusterMonitor = default!;

    /// <summary>
    /// Use this constructor to create a new instance.
    /// </summary>
    /// <param name="logger">The logger to use.</param>
    public AzureContainerAppsProvider(ILogger<AzureContainerAppsProvider> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task StartMemberAsync(Cluster cluster)
    {
        var clusterName = cluster.Config.ClusterName;
        var (host, port) = cluster.System.GetAddress();
        var kinds = cluster.GetClusterKinds();
        _cluster = cluster;
        _clusterName = clusterName;
        _host = host;
        _port = port;
        _kinds = kinds;
        
        // Start the monitor.
        StartClusterMonitor();
        
        // Register this member.
        RegisterMember();
        
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StartClientAsync(Cluster cluster)
    {
        _cluster = cluster;
        
        // Start the monitor.
        StartClusterMonitor();
        
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ShutdownAsync(bool graceful)
    {
        UnregisterMember();
        return Task.CompletedTask;
    }
    
    private void StartClusterMonitor()
    {
        // Create props for the cluster monitor actor.
        var props = _cluster.System.DI()
            .PropsFor<AzureContainerAppsClusterMonitor>()
            .WithGuardianSupervisorStrategy(Supervision.AlwaysRestartStrategy);

        // Spawn the cluster monitor actor.
        _clusterMonitor = _cluster.System.Root.SpawnNamedSystem(props, "$aca-cluster-monitor");
    }

    private void RegisterMember()
    {
        // Send a message to register this member, which will also start the cluster monitor.
        _cluster.System.Root.Send(_clusterMonitor,new RegisterMember(_clusterName, _host, _port, _kinds, _cluster.System.Id));
    }

    private void UnregisterMember()
    {
        _cluster.System.Root.Send(_clusterMonitor,new UnregisterMember());
    }
}