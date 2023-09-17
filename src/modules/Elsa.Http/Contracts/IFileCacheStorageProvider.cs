using FluentStorage.Blobs;

namespace Elsa.Http.Contracts;

/// <summary>
/// Represents a provider of a file cache storage.
/// </summary>
public interface IFileCacheStorageProvider
{
    /// <summary>
    /// Gets the storage.
    /// </summary>
    /// <returns></returns>
    IBlobStorage GetStorage();
}