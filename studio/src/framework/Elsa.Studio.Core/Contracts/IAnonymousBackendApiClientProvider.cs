using System.Diagnostics.CodeAnalysis;

namespace Elsa.Studio.Contracts;

/// <summary>
/// Provides API clients to the backend for anonymous (non-authenticated) calls.
/// </summary>
/// <remarks>
/// This provider is intended for endpoints like <c>/identity/login</c> where attaching an access token is not required
/// and can even be harmful (e.g., stale tokens, circular dependencies during sign-in).
/// </remarks>
public interface IAnonymousBackendApiClientProvider
{
    /// <summary>
    /// Gets the URL to the backend.
    /// </summary>
    Uri Url { get; }

    /// <summary>
    /// Gets an API client that does not attach access tokens.
    /// </summary>
    /// <typeparam name="T">The API client type.</typeparam>
    ValueTask<T> GetApiAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(CancellationToken cancellationToken = default) where T : class;
}
