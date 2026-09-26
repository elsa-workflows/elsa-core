using Microsoft.Extensions.Logging;

namespace Elsa.Logging.Contracts;

/// <summary>
/// Defines a contract for routing log entries to specific log sinks based on provided criteria.
/// </summary>
public interface ILogSinkRouter
{
    ValueTask WriteAsync(
        IEnumerable<string> sinkNames,
        string name,
        LogLevel level,
        string message,
        object? arguments,
        IDictionary<string, object?>? attributes = null,
        CancellationToken ct = default);
}