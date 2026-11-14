using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Elsa.Studio.Contracts;

namespace Elsa.Studio.Services;

/// <summary>
/// Default implementation of <see cref="IAnonymousBackendApiClientProvider"/>.
/// </summary>
public class DefaultAnonymousBackendApiClientProvider(
    IRemoteBackendAccessor remoteBackendAccessor,
    IBlazorServiceAccessor blazorServiceAccessor,
    IHttpClientFactory httpClientFactory,
    IServiceProvider serviceProvider) : IAnonymousBackendApiClientProvider
{
    /// <summary>
    /// The name of the named <see cref="HttpClient"/> reserved for anonymous backend calls.
    /// It is registered without authentication delegating handlers, so anonymous endpoints such as
    /// <c>/identity/login</c> never trigger token refresh when the backend responds with 401.
    /// </summary>
    public const string AnonymousHttpClientName = "Elsa.Studio.AnonymousBackendApi";

    /// <inheritdoc />
    public Uri Url => remoteBackendAccessor.RemoteBackend.Url;

    /// <inheritdoc />
    public ValueTask<T> GetApiAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(CancellationToken cancellationToken = default) where T : class
    {
        var backendUrl = remoteBackendAccessor.RemoteBackend.Url;

        // Use the dedicated anonymous client. The per-API named clients (e.g. "ILoginApi") are configured with the
        // authentication delegating handler, which refreshes tokens on 401 - behavior that must never apply to
        // anonymous calls such as the login endpoint.
        var httpClient = httpClientFactory.CreateClient(AnonymousHttpClientName);
        httpClient.BaseAddress = backendUrl;

        var client = ApiClientFactory.Create<T>(serviceProvider, httpClient);

        // Keep the Blazor scoped-service-provider behavior consistent.
        var decorator = DispatchProxy.Create<T, BlazorScopedProxyApi<T>>();
        (decorator as BlazorScopedProxyApi<T>)!.Initialize(client, blazorServiceAccessor, serviceProvider);

        return new(decorator);
    }
}
