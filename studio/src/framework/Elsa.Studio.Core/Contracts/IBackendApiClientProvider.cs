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
    /// Gets an opaque key that changes when the selected backend context changes, even if its URL is unchanged.
    /// </summary>
    string ContextKey => Url.ToString();

    /// <summary>
    /// Gets an API client from the backend connection provider.
    /// </summary>
    /// <typeparam name="T">The API client type.</typeparam>
    /// <returns>The API client.</returns>
    ValueTask<T> GetApiAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(CancellationToken cancellationToken = default) where T : class;
}
