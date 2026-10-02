using System.Diagnostics.CodeAnalysis;

namespace Elsa.Studio.Contracts;

/// <summary>
/// Provides connection details to the backend.
/// </summary>
public interface IBackendApiClientProvider
{
    /// <summary>
    /// Gets the URL to the backend.
    /// </summary>
    Uri Url { get; }

    /// <summary>
    /// Gets an API client from the backend connection provider.
    /// </summary>
    /// <typeparam name="T">The API client type.</typeparam>
    /// <returns>The API client.</returns>
    ValueTask<T> GetApiAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(CancellationToken cancellationToken = default) where T : class;
}