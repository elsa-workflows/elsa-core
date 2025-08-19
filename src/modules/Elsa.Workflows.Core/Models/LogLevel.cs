namespace Elsa.Workflows.Models;

/// <summary>
/// Defines log levels for the ProcessLogActivity.
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Trace log level - most verbose logging.
    /// </summary>
    Trace = 0,
    
    /// <summary>
    /// Debug log level - debugging information.
    /// </summary>
    Debug = 1,
    
    /// <summary>
    /// Information log level - general information.
    /// </summary>
    Information = 2,
    
    /// <summary>
    /// Warning log level - warning messages.
    /// </summary>
    Warning = 3,
    
    /// <summary>
    /// Error log level - error messages.
    /// </summary>
    Error = 4,
    
    /// <summary>
    /// Critical log level - critical errors.
    /// </summary>
    Critical = 5
}