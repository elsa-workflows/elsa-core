using Azure.ResourceManager;
using JetBrains.Annotations;
using Proto.Cluster.AzureContainerApps.ArmClientProviders;
using Proto.Cluster.AzureContainerApps.Contracts;

namespace Proto.Cluster.AzureContainerApps.Utils;

/// <summary>
/// Provides an <see cref="ArmClient"/> instance.
/// </summary>
[PublicAPI]
public static class ArmClientProviders
{
    /// <summary>
    /// A default <see cref="IArmClientProvider"/> that uses <see cref="Azure.Identity.DefaultAzureCredential"/>
    /// </summary>
    public static readonly DefaultAzureCredentialArmClientProvider DefaultAzureCredential = new();
}