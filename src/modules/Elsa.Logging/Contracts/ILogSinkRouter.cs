using Microsoft.Extensions.Logging;

namespace Elsa.Logging.Contracts;

public interface ILogSinkRouter
{
    ValueTask WriteAsync(
        IEnumerable<string> sinkNames,
        LogLevel level,
        string message,
        object? arguments,
        IDictionary<string, object?>? attributes = null,
        CancellationToken ct = default);
}