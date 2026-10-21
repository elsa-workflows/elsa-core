namespace Elsa.Studio.Contracts;

/// <summary>
/// Describes an authentication source that advances the backend request context when its identity changes.
/// </summary>
/// <remarks>
/// This is an optional contract for features that need to bind a write to the identity used when a
/// document was loaded. Implementations advance the generation atomically with observing an identity
/// or authorization change, before a token acquisition can return a token for that observation.
/// Consumers read the generation as the request authority; signaling the same circuit-scoped
/// <see cref="IPermissionRefreshSignal"/> observed by the backend client provider is supplemental invalidation.
/// </remarks>
public interface IBackendRequestContextEpochSource
{
    /// <summary>Gets the monotonically increasing identity epoch observed by this source.</summary>
    long IdentityGeneration { get; }

    /// <summary>Returns whether this source raises the specified circuit signal for identity changes.</summary>
    bool UsesPermissionRefreshSignal(IPermissionRefreshSignal signal);
}

/// <summary>
/// Optionally binds an authenticated backend request to the context that loaded a protected document.
/// </summary>
/// <remarks>
/// Legacy backend providers remain compatible but do not establish this stronger request boundary.
/// Features that require it must fail closed when the authenticated transport has not participated.
/// </remarks>
public interface IBackendRequestContextBinding
{
    /// <summary>Whether the active authenticated transport has established a reliable context epoch.</summary>
    bool SupportsAuthenticatedRequestContextBinding { get; }

    /// <summary>Whether this asynchronous call currently carries a binding for this provider.</summary>
    bool HasActiveRequestContextBinding { get; }

    /// <summary>Registers the epoch source observed by the actual authenticated request transport.</summary>
    void RegisterAuthenticatedRequestTransport(IBackendRequestContextEpochSource? epochSource);

    /// <summary>Starts a request binding to the expected backend context until the returned scope is disposed.</summary>
    IDisposable BindRequestContext(string expectedContextKey);

    /// <summary>Validates the active binding against the source that returned the request token.</summary>
    void ValidateBoundRequestContext(IBackendRequestContextEpochSource? requestEpochSource);
}

/// <summary>Indicates that an authenticated request no longer matches its bound backend context.</summary>
public sealed class BackendRequestContextChangedException() : InvalidOperationException(
    "The authenticated backend context changed before the request was dispatched.");
