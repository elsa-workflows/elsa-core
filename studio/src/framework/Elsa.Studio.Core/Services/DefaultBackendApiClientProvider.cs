using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Elsa.Studio.Contracts;
using Microsoft.AspNetCore.Components.Authorization;

namespace Elsa.Studio.Services;

/// <summary>
/// Provides API clients to the remote backend.
/// </summary>
public class DefaultBackendApiClientProvider : IBackendApiClientProvider, IDisposable
{
    private readonly IRemoteBackendAccessor _remoteBackendAccessor;
    private readonly IBlazorServiceAccessor _blazorServiceAccessor;
    private readonly IServiceProvider _serviceProvider;
    private readonly AuthenticationStateProvider? _authenticationStateProvider;
    private int _authenticationGeneration;

    public DefaultBackendApiClientProvider(
        IRemoteBackendAccessor remoteBackendAccessor,
        IBlazorServiceAccessor blazorServiceAccessor,
        IServiceProvider serviceProvider)
        : this(remoteBackendAccessor, blazorServiceAccessor, serviceProvider, null)
    {
    }

    public DefaultBackendApiClientProvider(
        IRemoteBackendAccessor remoteBackendAccessor,
        IBlazorServiceAccessor blazorServiceAccessor,
        IServiceProvider serviceProvider,
        AuthenticationStateProvider? authenticationStateProvider)
    {
        _remoteBackendAccessor = remoteBackendAccessor;
        _blazorServiceAccessor = blazorServiceAccessor;
        _serviceProvider = serviceProvider;
        _authenticationStateProvider = authenticationStateProvider;
        if (_authenticationStateProvider != null)
            _authenticationStateProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;
    }

    /// <inheritdoc />
    public Uri Url => _remoteBackendAccessor.RemoteBackend.Url;

    /// <inheritdoc />
    public string ContextKey => $"{_remoteBackendAccessor.ContextKey}|auth:{Volatile.Read(ref _authenticationGeneration)}";

    /// <inheritdoc />
    public ValueTask<T> GetApiAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(CancellationToken cancellationToken = default) where T : class
    {
        var backendUrl = _remoteBackendAccessor.RemoteBackend.Url;
        var client = ApiClientFactory.Create<T>(_serviceProvider, backendUrl);
        var decorator = DispatchProxy.Create<T, BlazorScopedProxyApi<T>>();
        (decorator as BlazorScopedProxyApi<T>)!.Initialize(client, _blazorServiceAccessor, _serviceProvider);
        return new(decorator);
    }

    public void Dispose()
    {
        if (_authenticationStateProvider != null)
            _authenticationStateProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
    }

    private void OnAuthenticationStateChanged(Task<AuthenticationState> _) => Interlocked.Increment(ref _authenticationGeneration);
}
