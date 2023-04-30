using System.Text;
using System.Text.Json;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceTagsMemberStore"/> class.
    /// </summary>
    /// <param name="armArmClientProvider">The <see cref="IArmClientProvider"/> to use.</param>
    /// <param name="options">The options for this store.</param>
    /// <param name="logger">The logger to use.</param>
    public ResourceTagsMemberStore(
        IArmClientProvider armArmClientProvider,
        IOptions<ResourceTagsMemberStoreOptions> options,
        ILogger<ResourceTagsMemberStore> logger)
    {
        _armClientProvider = armArmClientProvider;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
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

        var taggedMemberTags = allTags
            .Where(kvp => kvp.Key.StartsWith(ResourceTagNames.NamePrefix) && !kvp.Key.Contains(ResourceTagNames.KindPrefix))
            .Select(x => x);

        foreach (var serializedTaggedMember in taggedMemberTags)
        {
            var taggedMember = Deserialize(serializedTaggedMember.Value);
            var memberId = serializedTaggedMember.Key[ResourceTagNames.NamePrefix.Length..];
            var member = new Member
            {
                Id = memberId,
                Host = taggedMember.Host,
                Port = taggedMember.Port,
            };

            var kinds = allTags
                .Where(x => x.Key.StartsWith(ResourceTagNames.Prefix(member.Id, ResourceTagNames.KindPrefix)))
                .Select(x => x.Value);

            member.Kinds.AddRange(kinds);
            members.Add(member);
        }


        return members.ToArray();
    }

    /// <inheritdoc />
    public async ValueTask RegisterAsync(string clusterName, Member member, CancellationToken cancellationToken = default)
    {
        var taggedMember = new TaggedMember(member.Host, member.Port, clusterName);
        var serializedTaggedMember = Serialize(taggedMember);

        var tags = new Dictionary<string, string>
        {
            [ResourceTagNames.Prefix(member.Id)] = serializedTaggedMember
        };

        foreach (var kind in member.Kinds)
            tags[ResourceTagNames.PrefixKind(member.Id, kind)] = kind;

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

    /// <inheritdoc />
    public async ValueTask UnregisterAsync(string memberId, CancellationToken cancellationToken = default)
    {
        var resourceGroupName = _options.Value.ResourceGroupName;
        var resourceName = _options.Value.ResourceName;
        var resource = await GetContainerAppManagedEnvironmentResourceAsync(resourceGroupName, resourceName, cancellationToken).ConfigureAwait(false);
        var tagResource = resource!.GetTagResource();
        var resourceTag = new Tag();
        var existingTags = (await tagResource.GetAsync(cancellationToken).ConfigureAwait(false)).Value.Data.TagValues;
        var prefixedName = ResourceTagNames.Prefix(memberId);

        foreach (var tag in existingTags)
            if (!tag.Key.StartsWith(prefixedName))
                resourceTag.TagValues.Add(tag);

        await tagResource.CreateOrUpdateAsync(WaitUntil.Completed, new TagResourceData(resourceTag), cancellationToken).ConfigureAwait(false);
    }

    private async Task AddMemberTags(string resourceGroupName, string resourceName, Dictionary<string, string> newTags, CancellationToken cancellationToken)
    {
        //var resourceTag = new Tag();

        // foreach (var tag in newTags)
        //     resourceTag.TagValues.Add(tag);

        var resource = await GetContainerAppManagedEnvironmentResourceAsync(resourceGroupName, resourceName, cancellationToken).ConfigureAwait(false);
        //var tagResource = resource!.GetTagResource();
        //var existingTags = (await tagResource.GetAsync(cancellationToken).ConfigureAwait(false)).Value.Data.TagValues;
        var tags = resource.Data.Tags;

        foreach (var tag in newTags)
            tags[tag.Key] = tag.Value;
        
        // foreach (var tag in existingTags)
        //     resourceTag.TagValues.Add(tag);

        await resource.SetTagsAsync(tags, cancellationToken);
        //await tagResource.CreateOrUpdateAsync(WaitUntil.Completed, new TagResourceData(resourceTag), cancellationToken).ConfigureAwait(false);
    }

    private async Task<GenericResource?> GetResourceAsync(string resourceGroupName, string resourceName, CancellationToken cancellationToken)
    {
        var armClient = await GetArmClientAsync().ConfigureAwait(false);
        var subscriptionId = _options.Value.SubscriptionId;
        var resourceGroup = await armClient.GetResourceGroupByNameAsync(resourceGroupName, subscriptionId, cancellationToken).ConfigureAwait(false);
        var resource = resourceGroup.Value.GetGenericResources($"name eq '{resourceName}'", cancellationToken: cancellationToken).FirstOrDefault();
        return resource;
    }
    
    private async Task<ContainerAppManagedEnvironmentResource> GetContainerAppManagedEnvironmentResourceAsync(string resourceGroupName, string resourceName, CancellationToken cancellationToken)
    {
        var armClient = await GetArmClientAsync().ConfigureAwait(false);
        var subscriptionId = _options.Value.SubscriptionId;
        var resourceGroup = await armClient.GetResourceGroupByNameAsync(resourceGroupName, subscriptionId, cancellationToken).ConfigureAwait(false);
        var response = await resourceGroup.Value.GetContainerAppManagedEnvironmentAsync(resourceName, cancellationToken);
        return response.Value;
    }

    private static string Serialize(TaggedMember taggedMember) => JsonSerializer.Serialize(taggedMember);
    private static TaggedMember Deserialize(string json) => JsonSerializer.Deserialize<TaggedMember>(json)!;
    private async Task<ArmClient> GetArmClientAsync() => _armClient ??= await _armClientProvider.CreateClientAsync().ConfigureAwait(false);
}