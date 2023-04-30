using Azure.Identity;
using Azure.ResourceManager;
using JetBrains.Annotations;

namespace Proto.Cluster.AzureContainerApps;

/// <summary>
/// Provides an <see cref="ArmClient"/> instance using <see cref="Azure.Identity.DefaultAzureCredential"/>
/// </summary>
[PublicAPI]
public class DefaultAzureCredentialArmClientProvider : IArmClientProvider
{
    /// <inheritdoc />
    public ValueTask<ArmClient> CreateClientAsync(CancellationToken cancellationToken = default)
    {
        var client = new ArmClient(new DefaultAzureCredential());
        return new(client);
    }
}