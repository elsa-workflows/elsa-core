using Microsoft.Extensions.Options;
using Proto.Cluster.AzureContainerApps.Stores.ResourceTags;

namespace Proto.Cluster.AzureContainerApps;

/// <summary>
/// Validates the <see cref="ResourceTagsMemberStoreOptions"/> to ensure that the required options are provided.
/// </summary>
public class AzureContainerAppsProviderOptionsValidator : IPostConfigureOptions<AzureContainerAppsProviderOptions>
{
    /// <inheritdoc />
    public void PostConfigure(string name, AzureContainerAppsProviderOptions options)
    {
        if (string.IsNullOrEmpty(options.ResourceGroupName))
            throw new Exception("No resource group provided");
    }
}