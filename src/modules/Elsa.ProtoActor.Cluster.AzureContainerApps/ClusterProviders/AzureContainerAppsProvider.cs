using Azure;
using Azure.ResourceManager;
using Azure.ResourceManager.AppContainers;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Proto.Cluster.AzureContainerApps.Contracts;
using Proto.Cluster.AzureContainerApps.Options;
using Proto.Cluster.AzureContainerApps.Stores.ResourceTags;
using Proto.Cluster.AzureContainerApps.Utils;
using Proto.Remote;
using Proto.Utils;

namespace Proto.Cluster.AzureContainerApps.ClusterProviders;

/// <summary>
/// A cluster provider that uses Azure Container Apps to host the cluster.
/// </summary>
[PublicAPI]
public class AzureContainerAppsProvider : IClusterProvider
{
    private readonly IArmClientProvider _armClientProvider;
    private readonly IClusterMemberStore _clusterMemberStore;
    private readonly IOptions<AzureContainerAppsProviderOptions> _options;
    private readonly ISystemClock _systemClock;
    private readonly ILogger _logger;
    private readonly string? _containerAppName;
    private readonly string _revisionName;
    private readonly string _replicaName;
    private readonly string _advertisedHost;
    
    private string _memberId = default!;
    private string _address = default!;
    private Cluster _cluster = default!;
    private string _clusterName = default!;
    private string[] _kinds = default!;
    private int _port;
    private ArmClient _client = default!;
    private ContainerAppResource _containerApp = default!;

    /// <summary>
    /// Use this constructor to create a new instance.
    /// </summary>
    /// <param name="armClientProvider">An <see cref="IArmClientProvider"/> to create <see cref="ArmClient"/> instances.</param>
    /// <param name="clusterMemberStore">The store to use for storing member information.</param>
    /// <param name="options">The options for this provider.</param>
    /// <param name="systemClock">The system clock to use.</param>
    /// <param name="logger">The logger to use.</param>
    public AzureContainerAppsProvider(
        IArmClientProvider armClientProvider,
        IClusterMemberStore clusterMemberStore,
        IOptions<AzureContainerAppsProviderOptions> options,
        ISystemClock systemClock,
        ILogger<AzureContainerAppsProvider> logger)
    {
        _armClientProvider = armClientProvider;
        _clusterMemberStore = clusterMemberStore;
        _options = options;
        _systemClock = systemClock;
        _logger = logger;
        _containerAppName = Environment.GetEnvironmentVariable("CONTAINER_APP_NAME") ?? throw new Exception("No app name provided");
        _revisionName = Environment.GetEnvironmentVariable("CONTAINER_APP_REVISION") ?? throw new Exception("No app revision provided");
        _replicaName = Environment.GetEnvironmentVariable("HOSTNAME") ?? throw new Exception("No replica name provided");
        _advertisedHost = IPUtils.FindSmallestIpAddress().ToString(); 
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
        
        await LoadContainerAppAsync().ConfigureAwait(false);
        await RegisterMemberAsync().ConfigureAwait(false);
        StartClusterMonitor();
    }

    /// <inheritdoc />
    public async Task StartClientAsync(Cluster cluster)
    {
        var clusterName = cluster.Config.ClusterName;
        var (_, port) = cluster.System.GetAddress();
        _cluster = cluster;
        _clusterName = clusterName;
        _memberId = cluster.System.Id;
        _port = port;
        _kinds = Array.Empty<string>();
        
        await LoadContainerAppAsync().ConfigureAwait(false);
        StartClusterMonitor();
    }

    /// <inheritdoc />
    public async Task ShutdownAsync(bool graceful) => await DeregisterMemberAsync().ConfigureAwait(false);

    private async Task LoadContainerAppAsync()
    {
        var subscriptionId = _options.Value.SubscriptionId;
        var resourceGroupName = _options.Value.ResourceGroupName;
        var resourceGroup = await _client.GetResourceGroupByNameAsync(resourceGroupName, subscriptionId).ConfigureAwait(false);
        var response =await resourceGroup.GetContainerAppAsync(_containerAppName).ConfigureAwait(false);
        
        _containerApp = response.Value;
    }
    
    private async Task RegisterMemberAsync()
    {
        await Retry.Try(RegisterMemberInternal, retryCount: Retry.Forever, onError: OnError, onFailed: OnFailed).ConfigureAwait(false);

        void OnError(int attempt, Exception exception) => _logger.LogWarning(exception, "Failed to register service");
        void OnFailed(Exception exception) => _logger.LogError(exception, "Failed to register service");
    }

    /// <summary>
    /// Registers this member in the cluster.
    /// </summary>
    private async Task RegisterMemberInternal()
    {
        var canReceiveTraffic = await CanReceiveTrafficAsync().ConfigureAwait(false);
        
        if (!canReceiveTraffic)
        {
            _logger.LogInformation("Revision {RevisionName} is not active", _revisionName);
            return;
        }

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
    
    private async Task<bool> CanReceiveTrafficAsync()
    {
        var revision = await _containerApp.GetContainerAppRevisionAsync(_revisionName).ConfigureAwait(false);
        return revision.Value.Data.TrafficWeight.GetValueOrDefault() > 0;
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
                    
                    var now = _systemClock.UtcNow;

                    try
                    {
                        var ttl = _options.Value.PollInterval + _options.Value.MemberTimeToLive;
                        var storedMembers = (await _clusterMemberStore.ListAsync().ConfigureAwait(false)).ToList();
                        var activeMembers = storedMembers.Where(m => m.UpdatedAt + ttl >= now).ToList();
                        var expiredMembers = storedMembers.Where(m => m.UpdatedAt + ttl < now).ToList();

                        // Log the members we got from storage.
                        if (storedMembers.Any())
                        {
                            _logger.LogInformation("Got members {Members}", storedMembers.Count);
                            
                            foreach (var storedMember in storedMembers)
                                _logger.LogInformation("Member {MemberId} on {MemberAddress}", storedMember.Id, storedMember.Address);
                        }
                        else
                        {
                            _logger.LogWarning("Did not get any members from {Store}", storeName);
                        }

                        // Update the current member entry if it's still active.
                        var currentMember = activeMembers.FirstOrDefault(m => m.Id == _memberId);
                            
                        if(currentMember is not null)
                        {
                            // Check if the current revision is active.
                            var canReceiveTraffic = await CanReceiveTrafficAsync().ConfigureAwait(false);
                                
                            if (!canReceiveTraffic)
                            {
                                _logger.LogInformation("Revision {RevisionName} is not active", _revisionName);
                                expiredMembers.Add(currentMember);
                                activeMembers.Remove(currentMember);
                            }
                            else
                            {
                                _logger.LogInformation("Updating current member {MemberId} on {MemberAddress}", currentMember.Id, currentMember.Address);
                                currentMember = currentMember with { UpdatedAt = now };
                                await _clusterMemberStore.UpdateAsync(currentMember).ConfigureAwait(false);
                            }
                        }
                        
                        // Remove expired members from storage.
                        if(expiredMembers.Any())
                        {
                            _logger.LogInformation("Removing {Members} expired members", expiredMembers.Count);

                            foreach (var expiredMember in expiredMembers)
                            {
                                _logger.LogInformation("Expired member {MemberId} on {MemberAddress}", expiredMember.Id, expiredMember.Address);
                                await _clusterMemberStore.UnregisterAsync(expiredMember.Id).ConfigureAwait(false);
                            }
                        }

                        var members = activeMembers.Select(x => x.ToMember()).ToList();
                        _cluster.MemberList.UpdateClusterTopology(members);
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