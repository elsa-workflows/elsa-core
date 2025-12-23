using Elsa.Resilience.Contracts;

namespace Elsa.Resilience.Services;

/// <summary>
/// Default implementation that detects common transient exceptions from the .NET framework and common patterns.
/// </summary>
public class DefaultTransientExceptionDetector : ITransientExceptionDetector
{
    private static readonly HashSet<string> TransientExceptionTypeNames = new(StringComparer.OrdinalIgnoreCase)
    {
        // Common framework exceptions
        "HttpRequestException",
        "TimeoutException",
        "TaskCanceledException",
        "IOException",
        "SocketException",
        "EndOfStreamException",
        
        // Database-related transient exceptions (by name, not type reference)
        "DbException",
        "SqlException",
        "NpgsqlException",
        "MongoConnectionException",
        "MongoExecutionTimeoutException",
        "MongoNodeIsRecoveringException",
        "MongoNotPrimaryException",
        "MySqlException",
        
        // Network-related exceptions
        "HttpIOException",
        "WebException",
    };
    
    private static readonly HashSet<string> TransientExceptionMessagePatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        "timeout",
        "timed out",
        "connection reset",
        "connection refused",
        "broken pipe",
        "network",
        "end of stream",
        "attempted to read past the end",
        "the connection is closed",
        "connection is not open",
        "failed to connect",
        "no connection could be made",
        "an existing connection was forcibly closed",
    };

    /// <inheritdoc />
    public bool IsTransient(Exception exception)
    {
        // Check if the exception type name matches any known transient exception
        var exceptionTypeName = exception.GetType().Name;
        if (TransientExceptionTypeNames.Contains(exceptionTypeName))
            return true;
        
        // Check if the exception message contains any transient patterns
        if (!string.IsNullOrEmpty(exception.Message))
        {
            foreach (var pattern in TransientExceptionMessagePatterns)
            {
                if (exception.Message.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        
        return false;
    }
}
