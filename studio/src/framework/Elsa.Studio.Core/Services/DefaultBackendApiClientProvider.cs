using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Elsa.Studio.Contracts;
using Microsoft.AspNetCore.Components.Authorization;

namespace Elsa.Studio.Services;

/// <summary>
/// Provides API clients to the remote backend.
/// </summary>
public class DefaultBackendApiClientProvider : IBackendApiClientProvider, IBackendRequestContextBinding, IDisposable
{
    private readonly IRemoteBackendAccessor _remoteBackendAccessor;
    private readonly IBlazorServiceAccessor _blazorServiceAccessor;
    private readonly IServiceProvider _serviceProvider;
    private readonly AuthenticationStateProvider? _authenticationStateProvider;
    private readonly IPermissionRefreshSignal? _permissionRefreshSignal;
    private readonly AsyncLocal<RequestContextFrame?> _requestContext = new();
    private WeakReference<IBackendRequestContextEpochSource>? _authenticatedRequestEpochSource;
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
        : this(remoteBackendAccessor, blazorServiceAccessor, serviceProvider, authenticationStateProvider, null)
    {
    }

    public DefaultBackendApiClientProvider(
        IRemoteBackendAccessor remoteBackendAccessor,
        IBlazorServiceAccessor blazorServiceAccessor,
        IServiceProvider serviceProvider,
        AuthenticationStateProvider? authenticationStateProvider = null,
        IPermissionRefreshSignal? permissionRefreshSignal = null)
    {
        _remoteBackendAccessor = remoteBackendAccessor;
        _blazorServiceAccessor = blazorServiceAccessor;
        _serviceProvider = serviceProvider;
        _authenticationStateProvider = authenticationStateProvider;
        _permissionRefreshSignal = permissionRefreshSignal;
        if (_authenticationStateProvider != null)
            _authenticationStateProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;
        if (_permissionRefreshSignal != null)
            _permissionRefreshSignal.Raised += OnPermissionRefresh;
    }

    /// <inheritdoc />
    public Uri Url => _remoteBackendAccessor.RemoteBackend.Url;

    /// <inheritdoc />
    public string ContextKey => $"{_remoteBackendAccessor.ContextKey}|auth:{Volatile.Read(ref _authenticationGeneration)}";

    /// <inheritdoc />
    public bool SupportsAuthenticatedRequestContextBinding =>
        _permissionRefreshSignal != null &&
        Volatile.Read(ref _authenticatedRequestEpochSource) is { } epochSourceReference &&
        epochSourceReference.TryGetTarget(out var epochSource) &&
        epochSource.UsesPermissionRefreshSignal(_permissionRefreshSignal);

    /// <inheritdoc />
    public bool HasActiveRequestContextBinding => ReferenceEquals(_requestContext.Value?.Provider, this);

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
        if (_permissionRefreshSignal != null)
            _permissionRefreshSignal.Raised -= OnPermissionRefresh;
    }

    /// <inheritdoc />
    public void RegisterAuthenticatedRequestTransport(IBackendRequestContextEpochSource? epochSource)
    {
        if (_permissionRefreshSignal != null && epochSource?.UsesPermissionRefreshSignal(_permissionRefreshSignal) == true)
            Volatile.Write(ref _authenticatedRequestEpochSource, new WeakReference<IBackendRequestContextEpochSource>(epochSource));
        else
            Volatile.Write(ref _authenticatedRequestEpochSource, null);
    }

    /// <inheritdoc />
    public IDisposable BindRequestContext(string expectedContextKey)
    {
        if (!SupportsAuthenticatedRequestContextBinding)
            throw new InvalidOperationException("The authenticated backend connection cannot bind this write to its current identity.");

        if (!string.Equals(ContextKey, expectedContextKey, StringComparison.Ordinal))
            throw new BackendRequestContextChangedException();

        var previous = _requestContext.Value;
        var frame = new RequestContextFrame(this, expectedContextKey, previous);
        _requestContext.Value = frame;
        return new RequestContextScope(this, frame, previous);
    }

    /// <inheritdoc />
    public void ValidateBoundRequestContext()
    {
        var frame = _requestContext.Value;
        if (frame == null || !ReferenceEquals(frame.Provider, this))
            return;

        if (!SupportsAuthenticatedRequestContextBinding ||
            !string.Equals(ContextKey, frame.ExpectedContextKey, StringComparison.Ordinal))
            throw new BackendRequestContextChangedException();
    }

    private void OnAuthenticationStateChanged(Task<AuthenticationState> _) => Interlocked.Increment(ref _authenticationGeneration);

    private void OnPermissionRefresh() => Interlocked.Increment(ref _authenticationGeneration);

    private void RestoreRequestContext(RequestContextFrame frame, RequestContextFrame? previous)
    {
        if (ReferenceEquals(_requestContext.Value, frame))
            _requestContext.Value = previous;
    }

    private sealed record RequestContextFrame(DefaultBackendApiClientProvider Provider, string ExpectedContextKey, RequestContextFrame? Parent);

    private sealed class RequestContextScope(
        DefaultBackendApiClientProvider provider,
        RequestContextFrame frame,
        RequestContextFrame? previous) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                provider.RestoreRequestContext(frame, previous);
        }
    }
}
