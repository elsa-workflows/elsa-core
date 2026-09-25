using FluentStorage.Blobs;

namespace Elsa.Storage.Files.Services;

/// <summary>
/// A provider of <see cref="IBlobStorage"/>.
/// </summary>
public class BlobStorageProvider : IBlobStorageProvider
{
    private readonly IBlobStorage _blobStorage;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobStorageProvider"/> class.
    /// </summary>
    public BlobStorageProvider(IBlobStorage blobStorage)
    {
        _blobStorage = blobStorage;
    }

    /// <inheritdoc />
    public IBlobStorage GetBlobStorage() => _blobStorage;
}