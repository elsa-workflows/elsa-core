using FluentStorage.Blobs;

namespace Elsa.Http.Contracts;

/// <summary>
/// Represents a cache for storing files.
/// </summary>
public interface IFileCacheStorageProvider
{
    /// <summary>
    /// Gets the storage.
    /// </summary>
    /// <returns></returns>
    IBlobStorage GetStorage();
}