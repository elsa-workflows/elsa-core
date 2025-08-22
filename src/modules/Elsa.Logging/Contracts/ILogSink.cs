using Microsoft.Extensions.Logging;

namespace Elsa.Logging.Contracts;

public interface ILogSink
{
    string Name { get; }

    ValueTask WriteAsync(LogLevel level, string message, object? arguments, IDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default);
}