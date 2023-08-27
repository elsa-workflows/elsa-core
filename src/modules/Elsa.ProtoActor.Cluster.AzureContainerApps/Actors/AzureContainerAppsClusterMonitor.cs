using Azure.ResourceManager;
using Azure.ResourceManager.AppContainers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Proto.Cluster.AzureContainerApps.Contracts;
using Proto.Cluster.AzureContainerApps.Messages;
using Proto.Cluster.AzureContainerApps.Models;
using Proto.Cluster.AzureContainerApps.Options;
using Proto.Cluster.AzureContainerApps.Stores.ResourceTags;
using Proto.Timers;
using Proto.Utils;

namespace Proto.Cluster.AzureContainerApps.Actors;

/// <summary>
/// An actor that monitors periodically updates the member list. 
/// </summary>
public class AzureContainerAppsClusterMonitor : IActor
{
    private readonly Cluster _cluster;
    private readonly IArmClientProvider _armClientProvider;
    private readonly IClusterMemberStore _clusterMemberStore;
    private readonly IContainerAppMetadataAccessor _containerAppMetadataAccessor;
    private readonly ISystemClock _systemClock;
    private readonly IOptions<AzureContainerAppsProviderOptions> _options;
    private readonly ILogger _logger;

    private string _memberId = default!;
    private string _host = default!;
    private int _port;
    private string _address = default!;
    private ICollection<string> _kinds = default!;
    private ArmClient _client = default!;
    private ContainerAppMetadata _containerAppMetadata = default!;
    private ContainerAppResource _containerApp = default!;
    private CancellationTokenSource? _scheduledTask;
    private bool _stopping;

    /// <summary>
    /// Initializes a new instance of <see cref="AzureContainerAppsClusterMonitor"/>.
    /// </summary>
    public AzureContainerAppsClusterMonitor(
        Cluster cluster,
        IArmClientProvider armClientProvider,
        IClusterMemberStore clusterMemberStore,
        IContainerAppMetadataAccessor containerAppMetadataAccessor,
        ISystemClock systemClock,
        IOptions<AzureContainerAppsProviderOptions> options,
        ILogger<AzureContainerAppsClusterMonitor> logger
    )
    {
        _cluster = cluster;
        _armClientProvider = armClientProvider;
        _clusterMemberStore = clusterMemberStore;
        _containerAppMetadataAccessor = containerAppMetadataAccessor;
        _systemClock = systemClock;
        _options = options;
        _logger = logger;
    }

    private string ClusterName => _cluster.Config.ClusterName;

    /// <inheritdoc />
    public Task ReceiveAsync(IContext context)
    {
        var cancellationToken = context.CancellationToken;

        var task = context.Message switch
        {
            Started => OnStarted(cancellationToken),
            RegisterMember command => OnRegisterMember(context, command),
            UpdateMembers => OnUpdateMembers(context),
            UnregisterMember => OnUnregisterMember(),
            _ => Task.CompletedTask
        };

        return task;
    }

    private async Task OnStarted(CancellationToken cancellationToken)
    {
        _containerAppMetadata = await _containerAppMetadataAccessor.GetMetadataAsync(cancellationToken).ConfigureAwait(false);
        _client = await _armClientProvider.CreateClientAsync(cancellationToken).ConfigureAwait(false);
        await LoadContainerAppAsync(_containerAppMetadata.ContainerAppName).ConfigureAwait(false);
    }

    private async Task OnRegisterMember(IContext context, RegisterMember command)
    {
        // Store the member ID.
        _memberId = command.MemberId;
        _host = command.Host;
        _port = command.Port;
        _kinds = command.Kinds;
        _address = $"{_host}:{_port}";
        
        // Register the member in the store.
        await AddMember().ConfigureAwait(false);

        // Schedule the first update.
        ScheduleUpdate(context);
    }
    
    private async Task OnUnregisterMember()
    {
        await Retry.Try(UnregisterMemberInternal, onError: OnError, onFailed: OnFailed).ConfigureAwait(false);
        void OnError(int attempt, Exception exception) => _logger.LogWarning(exception, "Failed to unregister member");
        void OnFailed(Exception exception) => _logger.LogError(exception, "Failed to unregister member");
    }

    private async Task OnUpdateMembers(IContext context)
    {
        if (_stopping)
            return;

        var storeName = _clusterMemberStore.GetType().Name;
        var revisionName = _containerAppMetadata.RevisionName;

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

            if (currentMember is not null)
            {
                // Check if the current revision is active.
                var canReceiveTraffic = await CanReceiveTrafficAsync().ConfigureAwait(false);

                if (!canReceiveTraffic)
                {
                    _logger.LogInformation("Revision {RevisionName} is not active", revisionName);
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
            if (expiredMembers.Any())
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
        finally
        {
            // Schedule the next update.
            ScheduleUpdate(context);
        }
    }
    
    private async Task AddMember()
    {
        await Retry.Try(AddMemberInternal, retryCount: Retry.Forever, onError: OnError, onFailed: OnFailed).ConfigureAwait(false);

        void OnError(int attempt, Exception exception) => _logger.LogWarning(exception, "Failed to register member");
        void OnFailed(Exception exception) => _logger.LogError(exception, "Failed to register member");
    }
    
    private async Task AddMemberInternal()
    {
        var canReceiveTraffic = await CanReceiveTrafficAsync().ConfigureAwait(false);

        if (!canReceiveTraffic)
        {
            _logger.LogInformation("Revision {RevisionName} is not active", _containerAppMetadata.RevisionName);
            return;
        }

        var member = new Member
        {
            Id = _memberId,
            Host = _host,
            Port = _port,
        };

        _logger.LogInformation(
            "[Cluster][AzureContainerAppsProvider] Registering service {ReplicaName} on {IpAddress}",
            _containerAppMetadata.ReplicaName,
            _address);

        member.Kinds.AddRange(_kinds);

        await _clusterMemberStore.RegisterAsync(ClusterName, member).ConfigureAwait(false);
    }
    
    private void ScheduleUpdate(IContext context)
    {
        if(_stopping)
            return;
        
        var pollInterval = _options.Value.PollInterval;
        _scheduledTask = context.Scheduler().SendOnce(pollInterval, context.Self, new UpdateMembers());
    }

    private async Task<bool> CanReceiveTrafficAsync()
    {
        var revisionName = _containerAppMetadata.RevisionName;
        var revision = await _containerApp.GetContainerAppRevisionAsync(revisionName).ConfigureAwait(false);
        return revision.Value.Data.TrafficWeight.GetValueOrDefault() > 0;
    }
    
    private async Task LoadContainerAppAsync(string containerAppName)
    {
        var subscriptionId = _options.Value.SubscriptionId;
        var resourceGroupName = _options.Value.ResourceGroupName;
        var resourceGroup = await _client.GetResourceGroupByNameAsync(resourceGroupName, subscriptionId).ConfigureAwait(false);
        var response = await resourceGroup.GetContainerAppAsync(containerAppName).ConfigureAwait(false);

        _containerApp = response.Value;
    }
    
    private async Task UnregisterMemberInternal()
    {
        _stopping = true;
        _scheduledTask?.Cancel();
        _logger.LogInformation("[Cluster][AzureContainerAppsProvider] Unregistering member {ReplicaName} on {IpAddress}", _containerAppMetadata.RevisionName, _address);
        await _clusterMemberStore.UnregisterAsync(_memberId).ConfigureAwait(false);
    }
}