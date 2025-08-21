using Microsoft.Extensions.Logging;

namespace Elsa.Logging.Contracts;

public interface ILogSinkRouter
{
    ValueTask WriteAsync(
        IEnumerable<string> targetNames,
        LogLevel level,
        string message,
        IReadOnlyDictionary<string, object?>? properties = null,
        CancellationToken ct = default);
}