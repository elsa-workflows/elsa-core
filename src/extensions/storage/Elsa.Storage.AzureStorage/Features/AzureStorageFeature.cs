using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Storage.AzureStorage.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Storage.AzureStorage.Features;

/// <summary>
/// Represents a feature for setting up Azure Storage integration within the Elsa framework.
/// </summary>
/// <remarks>
/// Registers and configures necessary services to work with Azure Blob Storage,
/// including factories for creating BlobServiceClient and BlobContainerClient instances.
/// </remarks>
public class AzureStorageFeature(IModule module) : FeatureBase(module)
{
    /// <inheritdoc />
    public override void Apply()
    {
        Services
            .AddSingleton<BlobServiceClientFactory>()
            .AddSingleton<BlobContainerClientFactory>();
    }
}