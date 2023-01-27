using Azure;
using Azure.ResourceManager;
using Azure.ResourceManager.AppContainers;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using Microsoft.Extensions.Logging;
using Proto;
using Proto.Cluster;

namespace Elsa.ProtoActor.Cluster.AzureContainerApps;

public static class ArmClientUtils
{
    private static readonly ILogger Logger = Log.CreateLogger(nameof(ArmClientUtils));

    public static async Task<Member[]> GetClusterMembers(this ArmClient client, string resourceGroupName, string containerAppName)
    {
        var members = new List<Member>();

        var containerApp = await (await client.GetResourceGroupByName(resourceGroupName)).Value.GetContainerAppAsync(containerAppName);

        if (containerApp is null || !containerApp.HasValue)
        {
            Logger.LogError("Container App: {ContainerApp} in resource group: {ResourceGroup} is not found", containerApp, resourceGroupName);
            return members.ToArray();
        }

        var containerAppRevisions = GetActiveRevisionsWithTraffic(containerApp).ToList();
        if (!containerAppRevisions.Any())
        {
            Logger.LogError("Container App: {ContainerApp} in resource group: {ResourceGroup} does not contain any active revisions with traffic", containerAppName, resourceGroupName);
            return members.ToArray();
        }

        var replicasWithTraffic = containerAppRevisions.SelectMany(r => r.GetContainerAppReplicas());

        var allTags = (await containerApp.Value.GetTagResource().GetAsync()).Value.Data.TagValues;

        foreach (var replica in replicasWithTraffic)
        {
            var replicaNameTag = allTags.FirstOrDefault(kvp => kvp.Value == replica.Data.Name);
            if (replicaNameTag.Key == null)
            {
                Logger.LogWarning("Skipping Replica with name: {Name}, no Proto Tags found", replica.Data.Name);
                continue;
            }

            var replicaNameTagPrefix = replicaNameTag.Key.Replace(ResourceTagLabels.LabelReplicaNameWithoutPrefix, string.Empty);
            var currentReplicaTags = allTags.Where(kvp => kvp.Key.StartsWith(replicaNameTagPrefix)).ToDictionary(x => x.Key, x => x.Value);

            var memberId = currentReplicaTags.FirstOrDefault(kvp => kvp.Key.ToString().Contains(ResourceTagLabels.LabelMemberIdWithoutPrefix)).Value;

            var kinds = currentReplicaTags
                .Where(kvp => kvp.Key.StartsWith(ResourceTagLabels.LabelKind(memberId)))
                .Select(kvp => kvp.Key[(ResourceTagLabels.LabelKind(memberId).Length + 1)..])
                .ToArray();

            var member = new Member
            {
                Id = currentReplicaTags[ResourceTagLabels.LabelMemberId(memberId)],
                Port = int.Parse(currentReplicaTags[ResourceTagLabels.LabelPort(memberId)]),
                Host = currentReplicaTags[ResourceTagLabels.LabelHost(memberId)],
                Kinds = { kinds }
            };

            members.Add(member);
        }

        return members.ToArray();
    }

    public static async Task AddMemberTags(this ArmClient client, string resourceGroupName, string containerAppName, Dictionary<string, string> newTags)
    {
        var resourceTag = new Tag();
        foreach (var tag in newTags)
        {
            resourceTag.TagValues.Add(tag);
        }

        var resourceGroup = await client.GetResourceGroupByName(resourceGroupName);
        var containerApp = await resourceGroup.Value.GetContainerAppAsync(containerAppName);
        var tagResource = containerApp.Value.GetTagResource();

        var existingTags = (await tagResource.GetAsync()).Value.Data.TagValues;
        foreach (var tag in existingTags)
        {
            resourceTag.TagValues.Add(tag);
        }

        await tagResource.CreateOrUpdateAsync(WaitUntil.Completed, new TagResourceData(resourceTag));
    }

    public static async Task ClearMemberTags(this ArmClient client, string resourceGroupName, string containerAppName, string memberId)
    {
        var resourceGroup = await client.GetResourceGroupByName(resourceGroupName);
        var containerApp = await resourceGroup.Value.GetContainerAppAsync(containerAppName);
        var tagResource = containerApp.Value.GetTagResource();

        var resourceTag = new Tag();
        var existingTags = (await tagResource.GetAsync()).Value.Data.TagValues;

        foreach (var tag in existingTags)
        {
            if (!tag.Key.StartsWith(ResourceTagLabels.LabelPrefix(memberId)))
            {
                resourceTag.TagValues.Add(tag);
            }
        }

        await tagResource.CreateOrUpdateAsync(WaitUntil.Completed, new TagResourceData(resourceTag));
    }

    public static async Task<Response<ResourceGroupResource>> GetResourceGroupByName(this ArmClient client, string resourceGroupName) =>
        await (await client.GetDefaultSubscriptionAsync()).GetResourceGroups().GetAsync(resourceGroupName);

    private static IEnumerable<ContainerAppRevisionResource> GetActiveRevisionsWithTraffic(ContainerAppResource containerApp) =>
        containerApp.GetContainerAppRevisions().Where(r => r.HasData && (r.Data.IsActive ?? false) && r.Data.TrafficWeight > 0);
}