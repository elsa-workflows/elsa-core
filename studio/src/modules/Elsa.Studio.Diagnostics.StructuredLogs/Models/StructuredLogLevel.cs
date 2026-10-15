namespace Elsa.Studio.Diagnostics.StructuredLogs.Models;

/// <summary>
/// Log levels emitted by the backend structured log stream.
/// </summary>
public enum StructuredLogLevel
{
    Trace = 0,
    Debug = 1,
    Information = 2,
    Warning = 3,
    Error = 4,
    Critical = 5,
    None = 6
}
