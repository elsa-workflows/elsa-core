namespace Elsa.Studio.Diagnostics.ConsoleLogs.Models;

/// <summary>
/// Describes the reported health of a console log source.
/// </summary>
public enum ConsoleLogSourceHealth
{
    /// <summary>
    /// Health is unknown.
    /// </summary>
    Unknown,

    /// <summary>
    /// Source is actively connected.
    /// </summary>
    Connected,

    /// <summary>
    /// Source is known but has not been seen recently.
    /// </summary>
    Stale,

    /// <summary>
    /// Source is disconnected.
    /// </summary>
    Disconnected
}
