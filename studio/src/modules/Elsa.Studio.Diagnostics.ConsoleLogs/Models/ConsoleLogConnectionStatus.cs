namespace Elsa.Studio.Diagnostics.ConsoleLogs.Models;

/// <summary>
/// Describes the live console stream connection state.
/// </summary>
public enum ConsoleLogConnectionStatus
{
    /// <summary>
    /// The stream is disconnected.
    /// </summary>
    Disconnected,

    /// <summary>
    /// The stream is connecting.
    /// </summary>
    Connecting,

    /// <summary>
    /// The stream is connected.
    /// </summary>
    Connected,

    /// <summary>
    /// The stream is reconnecting.
    /// </summary>
    Reconnecting,

    /// <summary>
    /// The backend feature is unavailable.
    /// </summary>
    Unavailable,

    /// <summary>
    /// The current user is unauthorized.
    /// </summary>
    Unauthorized,

    /// <summary>
    /// A non-auth API or stream error occurred.
    /// </summary>
    Error
}
