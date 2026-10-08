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
    IServiceProvider serviceProvider) : IAnonymousBackendApiClientProvider
{
    /// <inheritdoc />
    public Uri Url => remoteBackendAccessor.RemoteBackend.Url;

    /// <inheritdoc />
    public ValueTask<T> GetApiAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(CancellationToken cancellationToken = default) where T : class
    {
        var backendUrl = remoteBackendAccessor.RemoteBackend.Url;

        // Create a raw API client. This bypasses DI-resolved delegating handlers (like AuthenticatingApiHttpMessageHandler).
        var client = ApiClientFactory.Create<T>(serviceProvider, backendUrl);

        // Keep the Blazor scoped-service-provider behavior consistent.
        var decorator = DispatchProxy.Create<T, BlazorScopedProxyApi<T>>();
        (decorator as BlazorScopedProxyApi<T>)!.Initialize(client, blazorServiceAccessor, serviceProvider);

        return new(decorator);
    }
}
