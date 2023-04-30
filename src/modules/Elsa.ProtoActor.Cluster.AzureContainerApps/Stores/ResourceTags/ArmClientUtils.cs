using System.Threading.Tasks;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;

namespace Proto.Cluster.AzureContainerApps.Stores.ResourceTags;

/// <summary>
/// Adds extension methods to the <see cref="ArmClient"/> class.
/// </summary>
public static class ArmClientUtils
{
    /// <summary>
    /// Returns the specified resource group
    /// </summary>
    /// <param name="client">The <see cref="ArmClient"/> being extended.</param>
    /// <param name="resourceGroupName">The name of the resource group.</param>
    /// <param name="subscriptionId">The subscription ID. If not set, the default subscription will be used.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The resource group.</returns>
    public static async Task<ResourceGroupResource> GetResourceGroupByNameAsync(this ArmClient client, string resourceGroupName, string? subscriptionId = default, CancellationToken cancellationToken = default)
    {
        var resourceIdentifier = $"/subscriptions/{subscriptionId}";
        var subscription = subscriptionId != null ? client.GetSubscriptionResource(ResourceIdentifier.Parse(resourceIdentifier)) : await client.GetDefaultSubscriptionAsync(cancellationToken);
        var response = await subscription.GetResourceGroupAsync(resourceGroupName, cancellationToken).ConfigureAwait(false);
        return response.Value;
    }
}