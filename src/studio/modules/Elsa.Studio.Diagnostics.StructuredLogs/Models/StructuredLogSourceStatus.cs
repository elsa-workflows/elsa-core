namespace Elsa.Studio.Diagnostics.StructuredLogs.Models;

/// <summary>
/// Backend log source health status.
/// </summary>
public enum StructuredLogSourceStatus
{
    Unknown = 0,
    Connected = 1,
    Stale = 2,
    Disconnected = 3
}
