using Microsoft.Extensions.Logging;

namespace Elsa.Logging.Contracts;

public interface ILogSink
{
    string Name { get; }

    ValueTask WriteAsync(LogLevel level, string message, IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default);
}