using System;
using System.Linq;
using System.Threading.Tasks;
using Azure.ResourceManager;
using Azure.ResourceManager.AppContainers;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Proto.Cluster.AzureContainerApps.Stores.ResourceTags;
using Proto.Utils;

namespace Proto.Cluster.AzureContainerApps;

/// <summary>
/// A cluster provider that uses Azure Container Apps to host the cluster.
/// </summary>
[PublicAPI]
public class AzureContainerAppsProvider : IClusterProvider
{
    private readonly IArmClientProvider _armClientProvider;
    private readonly IClusterMemberStore _clusterMemberStore;
    private readonly IOptions<AzureContainerAppsProviderOptions> _options;
    private readonly ILogger _logger;
    private readonly string? _containerAppName;
    private readonly string _revisionName;
    private readonly string _replicaName;
    private readonly string _advertisedHost;
    
    private string _memberId = null!;
    private string _address = null!;
    private Cluster _cluster = null!;
    private string _clusterName = null!;
    private string[] _kinds = null!;
    private int _port;
    private ArmClient _client = null!;

    /// <summary>
    /// Use this constructor to create a new instance.
    /// </summary>
    /// <param name="armClientProvider">An <see cref="IArmClientProvider"/> to create <see cref="ArmClient"/> instances.</param>
    /// <param name="clusterMemberStore">The store to use for storing member information.</param>
    /// <param name="options">The options for this provider.</param>
    /// <param name="logger">The logger to use.</param>
    public AzureContainerAppsProvider(
        IArmClientProvider armClientProvider,
        IClusterMemberStore clusterMemberStore,
        IOptions<AzureContainerAppsProviderOptions> options,
        ILogger<AzureContainerAppsProvider> logger)
    {
        _armClientProvider = armClientProvider;
        _clusterMemberStore = clusterMemberStore;
        _options = options;
        _logger = logger;
        _containerAppName = Environment.GetEnvironmentVariable("CONTAINER_APP_NAME") ?? throw new Exception("No app name provided");
        _revisionName = Environment.GetEnvironmentVariable("CONTAINER_APP_REVISION") ?? throw new Exception("No app revision provided");
        _replicaName = Environment.GetEnvironmentVariable("HOSTNAME") ?? throw new Exception("No replica name provided");
        _advertisedHost = ConfigUtils.FindSmallestIpAddress().ToString(); 
    }

    /// <inheritdoc />
    public async Task StartMemberAsync(Cluster cluster)
    {
        var clusterName = cluster.Config.ClusterName;
        var (host, port) = cluster.System.GetAddress();
        var kinds = cluster.GetClusterKinds();
        _cluster = cluster;
        _clusterName = clusterName;
        _memberId = cluster.System.Id;
        _port = port;
        _kinds = kinds;
        _address = $"{host}:{port}";
        _client = await _armClientProvider.CreateClientAsync();

        //await CleanupStoreAsync(cluster);
        await RegisterMemberAsync().ConfigureAwait(false);
        StartClusterMonitor();
    }

    /// <inheritdoc />
    public Task StartClientAsync(Cluster cluster)
    {
        var clusterName = cluster.Config.ClusterName;
        var (_, port) = cluster.System.GetAddress();
        _cluster = cluster;
        _clusterName = clusterName;
        _memberId = cluster.System.Id;
        _port = port;
        _kinds = Array.Empty<string>();

        StartClusterMonitor();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task ShutdownAsync(bool graceful) => await DeregisterMemberAsync().ConfigureAwait(false);
    
    private async Task CleanupStoreAsync(Cluster cluster)
    {
        await _clusterMemberStore.ClearAsync(cluster.Config.ClusterName);
    }

    private async Task RegisterMemberAsync()
    {
        await Retry.Try(RegisterMemberInternal, retryCount: Retry.Forever, onError: OnError, onFailed: OnFailed).ConfigureAwait(false);

        void OnError(int attempt, Exception exception) => _logger.LogWarning(exception, "Failed to register service");
        void OnFailed(Exception exception) => _logger.LogError(exception, "Failed to register service");
    }

    private async Task RegisterMemberInternal()
    {
        var subscriptionId = _options.Value.SubscriptionId;
        var resourceGroupName = _options.Value.ResourceGroupName;
        var resourceGroup = await _client.GetResourceGroupByNameAsync(resourceGroupName, subscriptionId).ConfigureAwait(false);
        var containerApp = await resourceGroup.GetContainerAppAsync(_containerAppName).ConfigureAwait(false);
        var revision = await containerApp.Value.GetContainerAppRevisionAsync(_revisionName).ConfigureAwait(false);

        if ((revision.Value.Data.TrafficWeight ?? 0) == 0)
            return;

        var member = new Member
        {
            Id = _memberId,
            Host = _advertisedHost,
            Port = _port,
        };

        _logger.LogInformation(
            "[Cluster][AzureContainerAppsProvider] Registering service {ReplicaName} on {IpAddress}",
            _replicaName,
            _address);

        member.Kinds.AddRange(_kinds);
        await _clusterMemberStore.RegisterAsync(_clusterName, member).ConfigureAwait(false);
    }

    private void StartClusterMonitor()
    {
        var pollInterval = _options.Value.PollInterval;
        var storeName = _clusterMemberStore.GetType().Name;

        _ = SafeTask.Run(async () =>
            {
                while (!_cluster.System.Shutdown.IsCancellationRequested)
                {
                    _logger.LogInformation("Looking for members in {Store}", storeName);

                    try
                    {
                        var members = (await _clusterMemberStore.ListAsync().ConfigureAwait(false)).ToArray();

                        if (members.Any())
                        {
                            _logger.LogInformation("Got members {Members}", members.Length);
                            _cluster.MemberList.UpdateClusterTopology(members);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to get members from {Store}", storeName);
                        }
                    }
                    catch (Exception x)
                    {
                        _logger.LogError(x, "Failed to get members from {Store}", storeName);
                    }

                    await Task.Delay(pollInterval).ConfigureAwait(false);
                }
            }
        );
    }

    private async Task DeregisterMemberAsync()
    {
        await Retry.Try(DeregisterMemberInner, onError: OnError, onFailed: OnFailed).ConfigureAwait(false);
        void OnError(int attempt, Exception exception) => _logger.LogWarning(exception, "Failed to deregister service");
        void OnFailed(Exception exception) => _logger.LogError(exception, "Failed to deregister service");
    }

    private async Task DeregisterMemberInner()
    {
        _logger.LogInformation("[Cluster][AzureContainerAppsProvider] Unregistering member {ReplicaName} on {IpAddress}", _replicaName, _address);
        await _clusterMemberStore.UnregisterAsync(_memberId).ConfigureAwait(false);
    }
}