namespace Elsa.Studio.Diagnostics.ConsoleLogs.Models;

/// <summary>
/// Identifies the process stream that produced a console line.
/// </summary>
public enum ConsoleLogStream
{
    /// <summary>
    /// Standard output.
    /// </summary>
    Stdout,

    /// <summary>
    /// Standard error.
    /// </summary>
    Stderr
}
