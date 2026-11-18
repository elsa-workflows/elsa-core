using FluentStorage.Storage;

namespace Elsa.Http.FileCaches;

/// <summary>
/// A file cache that stores files in blob storage using FluentStorage.
/// </summary>
public class BlobFileCacheStorageProvider : IFileCacheStorageProvider
{
    private readonly IStore _blobStorage;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobFileCacheStorageProvider"/> class.
    /// </summary>
    public BlobFileCacheStorageProvider(IStore blobStorage)
    {
        _blobStorage = blobStorage;
    }

    /// <inheritdoc />
    public IStore GetStorage()
    {
        return _blobStorage;
    }
}