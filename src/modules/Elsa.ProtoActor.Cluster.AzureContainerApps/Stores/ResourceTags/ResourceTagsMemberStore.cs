using Azure;
using Azure.ResourceManager;
using Azure.ResourceManager.AppContainers;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Proto.Cluster.AzureContainerApps.Stores.ResourceTags;

/// <summary>
/// Stores members in the form of resource tags of the Azure Container Application resource.
/// </summary>
[PublicAPI]
public class ResourceTagsMemberStore : IMemberStore
{
    private readonly IArmClientProvider _armClientProvider;
    private readonly IOptions<ResourceTagsMemberStoreOptions> _options;
    private readonly ILogger _logger;

    private ArmClient? _armClient;

    public ResourceTagsMemberStore(
        IArmClientProvider armArmClientProvider,
        IOptions<ResourceTagsMemberStoreOptions> options,
        ILogger<ResourceTagsMemberStore> logger)
    {
        _armClientProvider = armArmClientProvider;
        _options = options;
        _logger = logger;
    }

    public async ValueTask<ICollection<Member>> ListAsync(CancellationToken cancellationToken = default)
    {
        var members = new List<Member>();
        var resourceGroupName = _options.Value.ResourceGroupName;
        var resourceName = _options.Value.ResourceName;
        var resource = await GetResourceAsync(resourceGroupName, resourceName, cancellationToken).ConfigureAwait(false);

        if (resource == null)
        {
            _logger.LogError("Resource: {ResourceName} in resource group: {ResourceGroup} is not found", resourceName, resourceGroupName);
            return members.ToArray();
        }

        var allTags = resource.Data.Tags;
        var memberId = allTags.FirstOrDefault(kvp => kvp.Key.Contains(ResourceTagLabels.LabelMemberIdWithoutPrefix)).Value;

        var kinds = allTags
            .Where(kvp => kvp.Key.StartsWith(ResourceTagLabels.LabelKind(memberId)))
            .Select(kvp => kvp.Key[(ResourceTagLabels.LabelKind(memberId).Length + 1)..])
            .ToArray();

        var member = new Member
        {
            Id = allTags[ResourceTagLabels.LabelMemberId(memberId)],
            Port = int.Parse(allTags[ResourceTagLabels.LabelPort(memberId)]),
            Host = allTags[ResourceTagLabels.LabelHost(memberId)],
            Kinds = { kinds }
        };

        members.Add(member);

        return members.ToArray();
    }

    public async ValueTask RegisterAsync(string clusterName, Member member, CancellationToken cancellationToken = default)
    {
        var tags = new Dictionary<string, string>
        {
            [ResourceTagLabels.LabelCluster(member.Id)] = clusterName,
            [ResourceTagLabels.LabelHost(member.Id)] = member.Host,
            [ResourceTagLabels.LabelPort(member.Id)] = member.Port.ToString(),
            [ResourceTagLabels.LabelMemberId(member.Id)] = member.Id,
        };

        foreach (var kind in member.Kinds)
        {
            var labelKey = $"{ResourceTagLabels.LabelKind(member.Id)}-{kind}";
            tags.TryAdd(labelKey, "true");
        }

        try
        {
            var resourceGroupName = _options.Value.ResourceGroupName;
            var resourceName = _options.Value.ResourceName;
            await AddMemberTags(resourceGroupName, resourceName, tags, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception x)
        {
            _logger.LogError(x, "Failed to update metadata");
        }
    }

    public async ValueTask UnregisterAsync(string memberId, CancellationToken cancellationToken = default)
    {
        var resourceGroupName = _options.Value.ResourceGroupName;
        var resourceName = _options.Value.ResourceName;
        var resource = await GetResourceAsync(resourceGroupName, resourceName, cancellationToken).ConfigureAwait(false);
        var tagResource = resource!.GetTagResource();
        var resourceTag = new Tag();
        var existingTags = (await tagResource.GetAsync(cancellationToken).ConfigureAwait(false)).Value.Data.TagValues;

        foreach (var tag in existingTags)
        {
            if (!tag.Key.StartsWith(ResourceTagLabels.LabelPrefix(memberId)))
            {
                resourceTag.TagValues.Add(tag);
            }
        }

        await tagResource.CreateOrUpdateAsync(WaitUntil.Completed, new TagResourceData(resourceTag), cancellationToken).ConfigureAwait(false);
    }

    private async Task AddMemberTags(string resourceGroupName, string resourceName, Dictionary<string, string> newTags, CancellationToken cancellationToken)
    {
        var resourceTag = new Tag();
        foreach (var tag in newTags)
        {
            resourceTag.TagValues.Add(tag);
        }

        var resource = await GetResourceAsync(resourceGroupName, resourceName, cancellationToken).ConfigureAwait(false);
        var tagResource = resource!.GetTagResource();
        var existingTags = (await tagResource.GetAsync(cancellationToken).ConfigureAwait(false)).Value.Data.TagValues;

        foreach (var tag in existingTags)
        {
            resourceTag.TagValues.Add(tag);
        }

        await tagResource.CreateOrUpdateAsync(WaitUntil.Completed, new TagResourceData(resourceTag), cancellationToken).ConfigureAwait(false);
    }

    private async Task<GenericResource?> GetResourceAsync(string resourceGroupName, string resourceName, CancellationToken cancellationToken)
    {
        var armClient = await GetArmClientAsync().ConfigureAwait(false);
        var subscriptionId = _options.Value.SubscriptionId;
        var resourceGroup = await armClient.GetResourceGroupByNameAsync( resourceGroupName, subscriptionId, cancellationToken).ConfigureAwait(false);
        var resource = resourceGroup.Value.GetGenericResources($"name eq '{resourceName}'", cancellationToken: cancellationToken).FirstOrDefault();
        return resource;
    }
    
    private async Task<ArmClient> GetArmClientAsync() => _armClient ??= await _armClientProvider.CreateClientAsync().ConfigureAwait(false);
}