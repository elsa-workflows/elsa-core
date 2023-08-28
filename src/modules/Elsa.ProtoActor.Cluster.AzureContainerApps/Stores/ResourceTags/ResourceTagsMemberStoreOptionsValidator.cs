using System;
using Microsoft.Extensions.Options;

namespace Proto.Cluster.AzureContainerApps.Stores.ResourceTags;

/// <summary>
/// Validates the <see cref="ResourceTagsMemberStoreOptions"/> to ensure that the required options are provided.
/// </summary>
public class ResourceTagsMemberStoreOptionsValidator : IPostConfigureOptions<ResourceTagsMemberStoreOptions>
{
    /// <inheritdoc />
    public void PostConfigure(string name, ResourceTagsMemberStoreOptions options)
    {
        if (string.IsNullOrEmpty(options.ResourceGroupName))
            throw new Exception("No resource group provided");
    }
}