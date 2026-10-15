using Azure.Storage.Blobs;

namespace Elsa.Storage.AzureStorage.Services;

/// <summary>
/// A factory class responsible for managing and providing instances of <see cref="BlobServiceClient"/>.
/// Ensures that only one unique instance of <see cref="BlobServiceClient"/> is created per connection string.
/// </summary>
public class BlobServiceClientFactory
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Dictionary<string, BlobServiceClient> _blobServiceClients = new();

    /// <summary>
    /// Retrieves or creates an instance of the <see cref="BlobServiceClient"/> for the given connection string.
    /// Ensures that only one instance of <see cref="BlobServiceClient"/> is created for each unique connection string.
    /// </summary>
    /// <param name="connectionString">The connection string used to create or retrieve the <see cref="BlobServiceClient"/>.</param>
    /// <returns>A <see cref="BlobServiceClient"/> instance corresponding to the provided connection string.</returns>
    public BlobServiceClient GetBlobServiceClient(string connectionString)
    {
        if (_blobServiceClients.TryGetValue(connectionString, out var client))
            return client;
        
        try
        {
            _semaphore.Wait();
            
            // Check again in case another thread added the client while we were waiting.
            if (_blobServiceClients.TryGetValue(connectionString, out client))
                return client;
            
            var newClient = CreateBlobServiceClient(connectionString);
            _blobServiceClients[connectionString] = newClient;
            return newClient;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    private static BlobServiceClient CreateBlobServiceClient(string connectionString)
    {
        if (connectionString.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            var url = new Uri(connectionString);
            return new(url);
        }
        
        return new(connectionString);
    }
}