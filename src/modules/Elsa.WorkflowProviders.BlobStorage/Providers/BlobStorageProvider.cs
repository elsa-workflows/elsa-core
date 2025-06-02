using Elsa.WorkflowProviders.BlobStorage.Contracts;
using FluentStorage.Blobs;

namespace Elsa.WorkflowProviders.BlobStorage.Providers;

/// <summary>
/// A provider of <see cref="IBlobStorage"/>.
/// </summary>
public class BlobStorageProvider : IBlobStorageProvider
{
    private readonly IBlobStorage _blobStorage;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobStorageProvider"/> class.
    /// </summary>
    /// <param name="blobStorage">The <see cref="IBlobStorage"/>.</param>
    public BlobStorageProvider(IBlobStorage blobStorage)
    {
        _blobStorage = blobStorage;
    }

    /// <inheritdoc />
    public IBlobStorage GetBlobStorage() => _blobStorage;
}