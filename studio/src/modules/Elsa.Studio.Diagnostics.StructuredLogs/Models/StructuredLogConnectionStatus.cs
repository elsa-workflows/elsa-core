namespace Elsa.Studio.Diagnostics.StructuredLogs.Models;

/// <summary>
/// SignalR and backend availability state for the structured log viewer.
/// </summary>
public enum StructuredLogConnectionStatus
{
    Disconnected = 0,
    Connecting = 1,
    Connected = 2,
    Reconnecting = 3,
    Unavailable = 4,
    Unauthorized = 5
}
