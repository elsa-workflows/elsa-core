using FluentStorage.Storage;

namespace Elsa.Http;

/// <summary>
/// Represents a provider of a file cache storage.
/// </summary>
public interface IFileCacheStorageProvider
{
    /// <summary>
    /// Gets the storage.
    /// </summary>
    /// <returns></returns>
    IStore GetStorage();
}