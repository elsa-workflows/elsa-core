namespace Elsa.Studio.Contracts;

/// <summary>
/// A process-wide signal that the current principal's tokens or backend identity just changed
/// without an <c>AuthenticationStateChanged</c> notification (silent JWT refresh or environment switch).
/// </summary>
/// <remarks>
/// The signal has no dependencies so Environment and Identity modules can raise it without
/// constructing the permission cache (which would cycle through the backend client).
/// </remarks>
public interface IPermissionRefreshSignal
{
    /// <summary>Raised after a token refresh or environment switch that should drop cached grants.</summary>
    event Action? Raised;

    /// <summary>Notifies subscribers that cached grants are no longer valid.</summary>
    void Raise();
}
