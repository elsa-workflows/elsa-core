using Azure.ResourceManager;
using Azure.ResourceManager.AppContainers;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Proto;
using Proto.Cluster;
using Proto.Utils;

namespace Elsa.ProtoActor.Cluster.AzureContainerApps;

[PublicAPI]
public class AzureContainerAppsProvider : IClusterProvider
{
    private readonly ArmClient _client;
    private readonly string _resourceGroup;
    private readonly string _containerAppName;
    private readonly string _revisionName;
    private readonly string _replicaName;
    private readonly string _advertisedHost;

    private string _memberId = null!;
    private string _address = null!;
    private global::Proto.Cluster.Cluster _cluster = null!;
    private string _clusterName = null!;
    private string[] _kinds = null!;
    private int _port;

    private static readonly ILogger Logger = Log.CreateLogger<AzureContainerAppsProvider>();
    private static readonly TimeSpan PollIntervalInSeconds = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Use this constructor to create a new instance.
    /// </summary>
    /// <param name="client">An existing <see cref="ArmClient"/></param> instance that you need to bring yourself.
    /// <param name="resourceGroup">The resource group name containing your Azure Container App.</param>
    /// <param name="containerAppName">The name of the container app. If not specified, the CONTAINER_APP_NAME environment variable is used.</param>
    /// <param name="revision">The revision of the container app. If not specified, the CONTAINER_APP_REVISION environment variable is used.</param>
    /// <param name="replicaName">The replica name of the container app. If not specified, the HOSTNAME environment variable is used.</param>
    /// <param name="advertisedHost">The host or IP address of the container app. If not specified, will take the smallest local IP address (e.g. 127.0.0.1).</param>
    public AzureContainerAppsProvider(
        ArmClient client,
        string resourceGroup,
        string? containerAppName = default,
        string? revision = default,
        string? replicaName = default,
        string? advertisedHost = default)
    {
        _client = client;
        _resourceGroup = resourceGroup;
        _containerAppName = containerAppName ?? Environment.GetEnvironmentVariable("CONTAINER_APP_NAME") ?? throw new Exception("No app name provided");
        _revisionName = revision ?? Environment.GetEnvironmentVariable("CONTAINER_APP_REVISION") ?? throw new Exception("No app revision provided");
        _replicaName = replicaName ?? Environment.GetEnvironmentVariable("HOSTNAME")  ?? throw new Exception("No replica name provided");
        _advertisedHost = !string.IsNullOrEmpty(advertisedHost) ? advertisedHost : ConfigUtils.FindSmallestIpAddress().ToString();
    }

    public async Task StartMemberAsync(global::Proto.Cluster.Cluster cluster)
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

        await RegisterMemberAsync();
        StartClusterMonitor();
    }

    public Task StartClientAsync(global::Proto.Cluster.Cluster cluster)
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

    public async Task ShutdownAsync(bool graceful) => await DeregisterMemberAsync();

    private async Task RegisterMemberAsync()
    {
        await Retry.Try(RegisterMemberInner, retryCount: Retry.Forever, onError: OnError, onFailed: OnFailed);

        static void OnError(int attempt, Exception exception) => Logger.LogWarning(exception, "Failed to register service");
        static void OnFailed(Exception exception) => Logger.LogError(exception, "Failed to register service");
    }

    private async Task RegisterMemberInner()
    {
        var resourceGroup = await _client.GetResourceGroupByName(_resourceGroup);
        var containerApp = await resourceGroup.Value.GetContainerAppAsync(_containerAppName);
        var revision = await containerApp.Value.GetContainerAppRevisionAsync(_revisionName);

        if (revision.Value.Data.TrafficWeight.GetValueOrDefault(0) == 0)
            return;

        Logger.LogInformation(
            "[Cluster][AzureContainerAppsProvider] Registering service {ReplicaName} on {IpAddress}",
            _replicaName,
            _address);

        var tags = new Dictionary<string, string>
        {
            [ResourceTagLabels.LabelCluster(_memberId)] = _clusterName,
            [ResourceTagLabels.LabelHost(_memberId)] = _advertisedHost,
            [ResourceTagLabels.LabelPort(_memberId)] = _port.ToString(),
            [ResourceTagLabels.LabelMemberId(_memberId)] = _memberId,
            [ResourceTagLabels.LabelReplicaName(_memberId)] = _replicaName
        };

        foreach (var kind in _kinds)
        {
            var labelKey = $"{ResourceTagLabels.LabelKind(_memberId)}-{kind}";
            tags.TryAdd(labelKey, "true");
        }

        try
        {
            await _client.AddMemberTags(_resourceGroup, _containerAppName, tags);
        }
        catch (Exception x)
        {
            Logger.LogError(x, "Failed to update metadata");
        }
    }

    private void StartClusterMonitor() =>
        _ = SafeTask.Run(async () =>
            {
                while (!_cluster.System.Shutdown.IsCancellationRequested)
                {
                    Logger.LogInformation("Calling ACS API");

                    try
                    {
                        var members = await _client.GetClusterMembers(_resourceGroup, _containerAppName);

                        if (members.Any())
                        {
                            Logger.LogInformation("Got members {Members}", members.Length);
                            _cluster.MemberList.UpdateClusterTopology(members);
                        }
                        else
                        {
                            Logger.LogWarning("Failed to get members from Azure Container Apps");
                        }
                    }
                    catch (Exception x)
                    {
                        Logger.LogError(x, "Failed to get members from Azure Container Apps");
                    }

                    await Task.Delay(PollIntervalInSeconds);
                }
            }
        );

    private async Task DeregisterMemberAsync()
    {
        await Retry.Try(DeregisterMemberInner, onError: OnError, onFailed: OnFailed);

        static void OnError(int attempt, Exception exception) =>
            Logger.LogWarning(exception, "Failed to deregister service");

        static void OnFailed(Exception exception) => Logger.LogError(exception, "Failed to deregister service");
    }

    private async Task DeregisterMemberInner()
    {
        Logger.LogInformation(
            "[Cluster][AzureContainerAppsProvider] Unregistering member {ReplicaName} on {IpAddress}",
            _replicaName,
            _address);

        await _client.ClearMemberTags(_resourceGroup, _containerAppName, _memberId);
    }
}