using Azure.Storage.Blobs;

namespace Elsa.Storage.AzureStorage.Services;

/// <summary>
/// Provides a factory for creating and managing instances of <see cref="BlobContainerClient"/>.
/// </summary>
/// <remarks>
/// This class maintains a cache of <see cref="BlobContainerClient"/> instances keyed by
/// their connection string to avoid creating duplicate clients for the same configuration.
/// Thread-safety is ensured by using a semaphore when accessing or modifying the cache.
/// </remarks>
public class BlobContainerClientFactory
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Dictionary<string, BlobContainerClient> _blobContainerClients = new();

    /// <summary>
    /// Retrieves a BlobContainerClient for the specified connection string, creating a new instance
    /// if one does not already exist in the cache.
    /// </summary>
    /// <param name="connectionString">
    /// The connection string used to identify or create the BlobContainerClient.
    /// </param>
    /// <returns>
    /// A BlobContainerClient instance associated with the provided connection string.
    /// </returns>
    public BlobContainerClient GetBlobContainerClient(string connectionString)
    {
        if (_blobContainerClients.TryGetValue(connectionString, out var client))
            return client;

        try
        {
            _semaphore.Wait();

            // Check again in case another thread added the client while we were waiting.
            if (_blobContainerClients.TryGetValue(connectionString, out client))
                return client;

            var newClient = new BlobContainerClient(new(connectionString));
            _blobContainerClients[connectionString] = newClient;
            return newClient;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}