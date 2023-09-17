using Elsa.Http.Contracts;
using FluentStorage.Blobs;

namespace Elsa.Http.FileCaches;

/// <summary>
/// A file cache that stores files in blob storage using FluentStorage.
/// </summary>
public class BlobFileCacheStorageProvider : IFileCacheStorageProvider
{
    private readonly IBlobStorage _blobStorage;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobFileCacheStorageProvider"/> class.
    /// </summary>
    public BlobFileCacheStorageProvider(IBlobStorage blobStorage)
    {
        _blobStorage = blobStorage;
    }

    /// <inheritdoc />
    public IBlobStorage GetStorage()
    {
        return _blobStorage;
    }
}