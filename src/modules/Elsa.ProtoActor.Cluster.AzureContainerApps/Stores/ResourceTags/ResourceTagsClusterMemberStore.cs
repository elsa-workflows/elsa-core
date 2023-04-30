using System.Text.Json;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.AppContainers;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Proto.Cluster.AzureContainerApps.Stores.ResourceTags;

/// <summary>
/// Stores members in the form of resource tags of the Azure Container Application resource.
/// </summary>
[PublicAPI]
public class ResourceTagsClusterMemberStore : IClusterMemberStore
{
    private readonly IArmClientProvider _armClientProvider;
    private readonly ILogger _logger;
    private readonly string _containerAppName;
    private readonly string _resourceGroupName;
    private readonly string? _subscriptionId;
    
    private ArmClient? _armClient;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceTagsClusterMemberStore"/> class.
    /// </summary>
    /// <param name="armArmClientProvider">The <see cref="IArmClientProvider"/> to use.</param>
    /// <param name="options">The options for this store.</param>
    /// <param name="logger">The logger to use.</param>
    public ResourceTagsClusterMemberStore(
        IArmClientProvider armArmClientProvider,
        IOptions<ResourceTagsMemberStoreOptions> options,
        ILogger<ResourceTagsClusterMemberStore> logger) : this(armArmClientProvider, logger, options.Value.ResourceGroupName, options.Value.SubscriptionId)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceTagsClusterMemberStore"/> class.
    /// </summary>
    /// <param name="armArmClientProvider">The <see cref="IArmClientProvider"/> to use.</param>
    /// <param name="logger">The logger to use.</param>
    /// <param name="resourceGroupName">The name of the resource group.</param>
    /// <param name="subscriptionId">The subscription ID.</param>
    internal ResourceTagsClusterMemberStore(
        IArmClientProvider armArmClientProvider,
        ILogger<ResourceTagsClusterMemberStore> logger,
        string resourceGroupName, 
        string? subscriptionId = default)
    {
        _armClientProvider = armArmClientProvider;
        _logger = logger;
        _resourceGroupName = resourceGroupName;
        _subscriptionId = subscriptionId;
        _containerAppName = Environment.GetEnvironmentVariable("CONTAINER_APP_NAME") ?? throw new Exception("No app name provided");
    }

    /// <inheritdoc />
    public async ValueTask<ICollection<Member>> ListAsync(CancellationToken cancellationToken = default)
    {
        var members = new List<Member>();
        var resourceGroupName = _resourceGroupName;
        var containerAppName = _containerAppName;
        var containerApp = await GetContainerAppAsync(cancellationToken).ConfigureAwait(false);

        if (containerApp == null)
        {
            _logger.LogError("Resource: {ResourceName} in resource group: {ResourceGroup} is not found", containerAppName, resourceGroupName);
            return members.ToArray();
        }

        // Get the app container managed environment in order to get the other container apps.
        var environmentId = containerApp.Data.EnvironmentId;
        var containerApps = (await GetContainerAppsAsync(environmentId, cancellationToken).ConfigureAwait(false)).ToList();
        
        // Build a list of all tags from all container apps.
        var allTags = containerApps.SelectMany(x => x.Data.Tags).ToList();

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
            await AddMemberTags(tags, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception x)
        {
            _logger.LogError(x, "Failed to update metadata");
        }
    }

    /// <inheritdoc />
    public async ValueTask UnregisterAsync(string memberId, CancellationToken cancellationToken = default)
    {
        var containerApp = await GetContainerAppAsync(cancellationToken).ConfigureAwait(false);
        
        if(containerApp == null)
            return;
        
        var existingTags = containerApp.Data.Tags;
        var prefixedName = ResourceTagNames.Prefix(memberId);

        foreach (var tag in existingTags)
            if (tag.Key.StartsWith(prefixedName))
                existingTags.Remove(tag.Key);

        await containerApp.SetTagsAsync(existingTags, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask ClearAsync(string clusterName, CancellationToken cancellationToken = default)
    {
        var containerApp = await GetContainerAppAsync(cancellationToken).ConfigureAwait(false);
        
        if(containerApp == null)
            return;
        
        var existingTags = containerApp.Data.Tags;
        var prefixedName = ResourceTagNames.NamePrefix;
        
        foreach (var tag in existingTags)
            if (tag.Key.StartsWith(prefixedName))
                existingTags.Remove(tag.Key);
        
        await containerApp.SetTagsAsync(existingTags, cancellationToken).ConfigureAwait(false);
    }

    private async Task AddMemberTags(Dictionary<string, string> newTags, CancellationToken cancellationToken)
    {
        var containerApp = await GetContainerAppAsync(cancellationToken).ConfigureAwait(false);
        
        if(containerApp == null)
            return;

        var tags = containerApp.Data.Tags;

        foreach (var tag in newTags)
            tags[tag.Key] = tag.Value;

        await containerApp.SetTagsAsync(tags, cancellationToken);
    }

    private async Task<ContainerAppResource?> GetContainerAppAsync(CancellationToken cancellationToken)
    {
        var armClient = await GetArmClientAsync().ConfigureAwait(false);
        var subscriptionId = _subscriptionId;
        var resourceGroupName = _resourceGroupName;
        var resourceGroup = await armClient.GetResourceGroupByNameAsync(resourceGroupName, subscriptionId, cancellationToken).ConfigureAwait(false);
        var resource = await resourceGroup.GetContainerAppAsync(_containerAppName, cancellationToken).ConfigureAwait(false);
        return resource.HasValue ? resource.Value : default;
    }
    
    private async Task<ContainerAppManagedEnvironmentResource> GetContainerAppManagedEnvironmentResourceAsync(ContainerAppResource containerApp, CancellationToken cancellationToken)
    {
        var armClient = await GetArmClientAsync().ConfigureAwait(false);
        var environmentId = containerApp.Data.EnvironmentId;
        var response = armClient.GetContainerAppManagedEnvironmentResource(environmentId);
        return response;
    }
    
    private async Task<IEnumerable<ContainerAppResource>> GetContainerAppsAsync(ResourceIdentifier environmentId, CancellationToken cancellationToken)
    {
        var armClient = await GetArmClientAsync();
        var resourceGroupName = _resourceGroupName;
        var subscriptionId = _subscriptionId;
        var resourceGroup = await armClient.GetResourceGroupByNameAsync(resourceGroupName, subscriptionId, cancellationToken).ConfigureAwait(false);
        return resourceGroup.GetContainerApps().Where(x => x.Data.EnvironmentId == environmentId);
    }
    
    private static IEnumerable<ContainerAppRevisionResource> GetActiveRevisionsWithTraffic(ContainerAppResource containerApp) =>
        containerApp.GetContainerAppRevisions().Where(r => r.HasData && (r.Data.IsActive ?? false) && r.Data.TrafficWeight > 0);

    private static string Serialize(TaggedMember taggedMember) => JsonSerializer.Serialize(taggedMember);
    private static TaggedMember Deserialize(string json) => JsonSerializer.Deserialize<TaggedMember>(json)!;
    private async Task<ArmClient> GetArmClientAsync() => _armClient ??= await _armClientProvider.CreateClientAsync().ConfigureAwait(false);
}