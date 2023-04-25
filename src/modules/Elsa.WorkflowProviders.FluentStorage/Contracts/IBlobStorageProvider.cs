using FluentStorage.Blobs;

namespace Elsa.WorkflowProviders.FluentStorage.Contracts;

/// <summary>
/// A provider of <see cref="IBlobStorage"/>. The point of this interface is to provide a wrapper for actual <see cref="IBlobStorage"/> implementations.
/// This prevents collisions when the application uses multiple <see cref="IBlobStorage"/> implementations.
/// </summary>
public interface IBlobStorageProvider
{
    /// <summary>
    /// Gets the <see cref="IBlobStorage"/>.
    /// </summary>
    /// <returns>The <see cref="IBlobStorage"/>.</returns>
    IBlobStorage GetBlobStorage();
}