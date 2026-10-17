using Elsa.WorkflowProviders.BlobStorage.Contracts;
using FluentStorage.Storage;

namespace Elsa.WorkflowProviders.BlobStorage.Providers;

/// <summary>
/// A provider of <see cref="IStore"/>.
/// </summary>
public class BlobStorageProvider : IBlobStorageProvider
{
    private readonly IStore _blobStorage;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobStorageProvider"/> class.
    /// </summary>
    /// <param name="blobStorage">The <see cref="IStore"/>.</param>
    public BlobStorageProvider(IStore blobStorage)
    {
        _blobStorage = blobStorage;
    }

    /// <inheritdoc />
    public IStore GetBlobStorage() => _blobStorage;
}