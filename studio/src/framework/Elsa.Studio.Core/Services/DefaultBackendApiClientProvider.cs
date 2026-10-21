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
    private AuthenticatedRequestTransportRegistration _authenticatedRequestTransport = new(0, null);
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
    public string ContextKey => CreateContextKey(Volatile.Read(ref _authenticatedRequestTransport));

    /// <inheritdoc />
    public bool SupportsAuthenticatedRequestContextBinding =>
        TryGetQualifiedEpochSource(Volatile.Read(ref _authenticatedRequestTransport), out _);

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
        if (_permissionRefreshSignal == null || epochSource?.UsesPermissionRefreshSignal(_permissionRefreshSignal) != true)
            epochSource = null;

        while (true)
        {
            var current = Volatile.Read(ref _authenticatedRequestTransport);
            if (IsSameEpochSource(current, epochSource))
                return;

            // The initial unqualified state and the first qualified transport share generation 0.
            // Every subsequent replacement or loss advances the generation, even if A is restored.
            var generation = current.Generation == 0 && current.EpochSourceReference == null && epochSource != null
                ? 0
                : checked(current.Generation + 1);
            var updated = new AuthenticatedRequestTransportRegistration(
                generation,
                epochSource == null ? null : new WeakReference<IBackendRequestContextEpochSource>(epochSource));

            if (ReferenceEquals(Interlocked.CompareExchange(ref _authenticatedRequestTransport, updated, current), current))
                return;
        }
    }

    /// <inheritdoc />
    public IDisposable BindRequestContext(string expectedContextKey)
    {
        var transport = Volatile.Read(ref _authenticatedRequestTransport);
        if (!TryGetQualifiedEpochSource(transport, out _))
            throw new InvalidOperationException("The authenticated backend connection cannot bind this write to its current identity.");

        if (!string.Equals(CreateContextKey(transport), expectedContextKey, StringComparison.Ordinal))
            throw new BackendRequestContextChangedException();

        var previous = _requestContext.Value;
        var frame = new RequestContextFrame(this, expectedContextKey, previous);
        _requestContext.Value = frame;
        return new RequestContextScope(this, frame, previous);
    }

    /// <inheritdoc />
    public void ValidateBoundRequestContext(IBackendRequestContextEpochSource? requestEpochSource)
    {
        var frame = _requestContext.Value;
        if (frame == null || !ReferenceEquals(frame.Provider, this))
            return;

        var transport = Volatile.Read(ref _authenticatedRequestTransport);
        if (!TryGetQualifiedEpochSource(transport, out var registeredEpochSource) ||
            requestEpochSource == null ||
            !ReferenceEquals(requestEpochSource, registeredEpochSource) ||
            !string.Equals(CreateContextKey(transport), frame.ExpectedContextKey, StringComparison.Ordinal))
            throw new BackendRequestContextChangedException();
    }

    private string CreateContextKey(AuthenticatedRequestTransportRegistration transport)
    {
        var identityGeneration = TryGetQualifiedEpochSource(transport, out var epochSource)
            ? epochSource.IdentityGeneration
            : 0;
        return $"{_remoteBackendAccessor.ContextKey}|auth:{Volatile.Read(ref _authenticationGeneration)}|transport:{transport.Generation}|identity:{identityGeneration}";
    }

    private bool TryGetQualifiedEpochSource(
        AuthenticatedRequestTransportRegistration transport,
        out IBackendRequestContextEpochSource epochSource)
    {
        epochSource = null!;
        if (_permissionRefreshSignal == null ||
            transport.EpochSourceReference is not { } epochSourceReference ||
            !epochSourceReference.TryGetTarget(out var target) ||
            !target.UsesPermissionRefreshSignal(_permissionRefreshSignal))
            return false;

        epochSource = target;
        return true;
    }

    private static bool IsSameEpochSource(
        AuthenticatedRequestTransportRegistration current,
        IBackendRequestContextEpochSource? epochSource)
    {
        if (epochSource == null)
            return current.EpochSourceReference == null;

        return current.EpochSourceReference is { } epochSourceReference &&
               epochSourceReference.TryGetTarget(out var currentEpochSource) &&
               ReferenceEquals(currentEpochSource, epochSource);
    }

    private void OnAuthenticationStateChanged(Task<AuthenticationState> _) => Interlocked.Increment(ref _authenticationGeneration);

    private void OnPermissionRefresh() => Interlocked.Increment(ref _authenticationGeneration);

    private void RestoreRequestContext(RequestContextFrame frame, RequestContextFrame? previous)
    {
        if (ReferenceEquals(_requestContext.Value, frame))
            _requestContext.Value = previous;
    }

    private sealed record AuthenticatedRequestTransportRegistration(
        long Generation,
        WeakReference<IBackendRequestContextEpochSource>? EpochSourceReference);

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
