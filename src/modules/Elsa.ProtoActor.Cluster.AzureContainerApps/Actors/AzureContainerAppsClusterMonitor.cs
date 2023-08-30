using Azure.ResourceManager.AppContainers;
using JetBrains.Annotations;
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
/// An actor that periodically updates the member list. 
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
    private ContainerAppMetadata _containerAppMetadata = default!;
    [CanBeNull] private ContainerAppResource _containerApp;
    [CanBeNull] private CancellationTokenSource _scheduledTask;
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
    }

    private Task OnRegisterMember(IContext context, RegisterMember command)
    {
        // Store the member ID.
        _memberId = command.MemberId;
        _host = command.Host;
        _port = command.Port;
        _kinds = command.Kinds;
        _address = $"{_host}:{_port}";

        var registerMemberTask = RegisterMemberInternal();

        // Reenter after the member has been registered.
        context.ReenterAfter(registerMemberTask, _ =>
        {
            // Schedule the first update.
            ScheduleUpdate(context);
        });

        return Task.CompletedTask;
    }

    private async Task OnUnregisterMember()
    {
        await Retry.Try(UnregisterMemberInternal, onError: OnError, onFailed: OnFailed).ConfigureAwait(false);
        void OnError(int attempt, Exception exception) => _logger.LogWarning(exception, "Failed to unregister member");
        void OnFailed(Exception exception) => _logger.LogError(exception, "Failed to unregister member");
    }

    private Task OnUpdateMembers(IContext context)
    {
        if (_stopping)
            return Task.CompletedTask;

        var updateMembersTask = UpdateMembersAsync();
        context.ReenterAfter(updateMembersTask, _ =>
        {
            // Schedule the next update.
            ScheduleUpdate(context);
        });

        return Task.CompletedTask;
    }

    private async Task RegisterMemberInternal()
    {
        // Register the member in the store.
        await AddMember().ConfigureAwait(false);
    }

    private async Task UpdateMembersAsync()
    {
        var storeName = _clusterMemberStore.GetType().Name;
        _logger.LogInformation("Looking for members in {Store}", storeName);

        var now = _systemClock.UtcNow;

        try
        {
            var ttl = _options.Value.PollInterval + _options.Value.MemberTimeToLive;
            var storedMembers = await GetMembersFromStore();
            var activeMembers = storedMembers.Where(m => m.UpdatedAt + ttl >= now).ToList();
            var expiredMembers = storedMembers.Where(m => m.UpdatedAt + ttl < now).ToList();

            LogStoredMembers(storedMembers);
            await UpdateCurrentMember(activeMembers, expiredMembers, now);
            await RemoveExpiredMembers(expiredMembers);

            UpdateClusterTopology(activeMembers);
        }
        catch (Exception x)
        {
            _logger.LogError(x, "Failed to get members from {Store}", storeName);
        }
    }

    private async Task<IList<StoredMember>> GetMembersFromStore()
    {
        return (await _clusterMemberStore.ListAsync().ConfigureAwait(false)).ToList();
    }

    private void LogStoredMembers(ICollection<StoredMember> storedMembers)
    {
        if (storedMembers.Any())
            _logger.LogInformation("Got members {Members}", storedMembers.Count);
        else
            _logger.LogWarning("Did not get any members from {Store}", _clusterMemberStore.GetType().Name);
    }

    private async Task UpdateCurrentMember(ICollection<StoredMember> activeMembers, ICollection<StoredMember> expiredMembers, DateTimeOffset now)
    {
        var currentMember = activeMembers.FirstOrDefault(m => m.Id == _memberId);

        if (currentMember is not null)
        {
            var canReceiveTraffic = await CanReceiveTrafficAsync().ConfigureAwait(false);
            var revisionName = _containerAppMetadata.RevisionName;

            if (!canReceiveTraffic)
            {
                _logger.LogInformation("Revision {RevisionName} is not active", revisionName);
                activeMembers.Remove(currentMember);
                expiredMembers.Add(currentMember);
            }
            else
            {
                _logger.LogInformation("Updating current member {MemberId} on {MemberAddress}", currentMember.Id, currentMember.Address);
                currentMember = currentMember with { UpdatedAt = now };
                await _clusterMemberStore.UpdateAsync(currentMember).ConfigureAwait(false);
            }
        }
    }

    private async Task RemoveExpiredMembers(IList<StoredMember> expiredMembers)
    {
        if (expiredMembers.Any())
        {
            _logger.LogInformation("Removing {Members} expired members", expiredMembers.Count);

            foreach (var expiredMember in expiredMembers)
            {
                _logger.LogInformation("Expired member {MemberId} on {MemberAddress}", expiredMember.Id, expiredMember.Address);
                await _clusterMemberStore.UnregisterAsync(expiredMember.Id).ConfigureAwait(false);
            }
        }
    }

    private void UpdateClusterTopology(IEnumerable<StoredMember> activeMembers)
    {
        var members = activeMembers.Select(x => x.ToMember()).ToList();
        _cluster.MemberList.UpdateClusterTopology(members);
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
        if (_stopping)
            return;

        var pollInterval = _options.Value.PollInterval;
        _scheduledTask = context.Scheduler().SendOnce(pollInterval, context.Self, new UpdateMembers());
    }

    private async Task<bool> CanReceiveTrafficAsync()
    {
        var containerApp = await GetContainerAppAsync();
        var revisionName = _containerAppMetadata.RevisionName;
        var revision = await containerApp.GetContainerAppRevisionAsync(revisionName).ConfigureAwait(false);
        return revision.Value.Data.TrafficWeight.GetValueOrDefault() > 0;
    }

    private async Task<ContainerAppResource> GetContainerAppAsync()
    {
        if (_containerApp is not null)
            return _containerApp;

        var subscriptionId = _options.Value.SubscriptionId;
        var resourceGroupName = _options.Value.ResourceGroupName;
        var client = await _armClientProvider.CreateClientAsync().ConfigureAwait(false);
        var resourceGroup = await client.GetResourceGroupByNameAsync(resourceGroupName, subscriptionId).ConfigureAwait(false);
        var containerAppName = _containerAppMetadata.ContainerAppName;
        var response = await resourceGroup.GetContainerAppAsync(containerAppName).ConfigureAwait(false);

        _containerApp = response.Value;
        return _containerApp;
    }

    private async Task UnregisterMemberInternal()
    {
        _stopping = true;
        _scheduledTask?.Cancel();
        _logger.LogInformation("[Cluster][AzureContainerAppsProvider] Unregistering member {ReplicaName} on {IpAddress}", _containerAppMetadata.RevisionName, _address);
        await _clusterMemberStore.UnregisterAsync(_memberId).ConfigureAwait(false);
    }
}