using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;

namespace Proto.Cluster.AzureContainerApps.Stores.ResourceTags;

public static class ArmClientUtils
{
    public static async Task<Response<ResourceGroupResource>> GetResourceGroupByNameAsync(this ArmClient client, string resourceGroupName, string? subscriptionId = default, CancellationToken cancellationToken = default)
    {
        var resourceIdentifier = $"/subscriptions/{subscriptionId}";
        var subscription = subscriptionId != null ? client.GetSubscriptionResource(ResourceIdentifier.Parse(resourceIdentifier)) : await client.GetDefaultSubscriptionAsync(cancellationToken);
        return await subscription.GetResourceGroupAsync(resourceGroupName, cancellationToken).ConfigureAwait(false);
    }
}