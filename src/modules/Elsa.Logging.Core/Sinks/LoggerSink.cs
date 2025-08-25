using Elsa.Logging.Contracts;
using Microsoft.Extensions.Logging;

namespace Elsa.Logging.Sinks;

/// <summary>
/// A Microsoft.Extensions.Logging (MEL) sink, built from a private LoggerFactory configured with any providers.
/// </summary>
public sealed class LoggerSink(string name, ILoggerFactory factory) : ILogSink
{
    /// <inheritdoc/>
    public string Name { get; } = name;

    /// <inheritdoc/>
    public ValueTask WriteAsync(string name, LogLevel level, string message, object? arguments, IDictionary<string, object?>? attributes = null, CancellationToken cancellationToken = default)
    {
        var l = factory.CreateLogger(name);

        if (!l.IsEnabled(level))
            return ValueTask.CompletedTask;

        using var scope = attributes is null ? null : l.BeginScope(attributes);
        l.Log(level, 0, null, message, arguments);
        return ValueTask.CompletedTask;
    }
}