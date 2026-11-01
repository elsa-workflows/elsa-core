using FluentStorage.Storage;

namespace Elsa.WorkflowProviders.BlobStorage.Contracts;

/// <summary>
/// A provider of <see cref="IStore"/>. The point of this interface is to provide a wrapper for actual <see cref="IStore"/> implementations.
/// This prevents collisions when the application uses multiple <see cref="IStore"/> implementations.
/// </summary>
public interface IBlobStorageProvider
{
    /// <summary>
    /// Gets the <see cref="IStore"/>.
    /// </summary>
    IStore GetBlobStorage();
}