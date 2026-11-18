using FluentStorage.Storage;

namespace Elsa.Storage.Files.Services;

/// <summary>
/// A provider of <see cref="IStore"/>.
/// </summary>
public class BlobStorageProvider : IBlobStorageProvider
{
    private readonly IStore _blobStorage;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobStorageProvider"/> class.
    /// </summary>
    public BlobStorageProvider(IStore blobStorage)
    {
        _blobStorage = blobStorage;
    }

    /// <inheritdoc />
    public IStore GetBlobStorage() => _blobStorage;
}