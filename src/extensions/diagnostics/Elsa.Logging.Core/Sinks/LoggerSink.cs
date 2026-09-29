using Elsa.Logging.Contracts;
using Elsa.Logging.Helpers;
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
        var logger = factory.CreateLogger(name);

        if (!logger.IsEnabled(level))
            return ValueTask.CompletedTask;

        var mappedArguments = LogArgumentHelper.ToArgumentsArray(arguments);
        using var scope = attributes is null ? null : logger.BeginScope(attributes);
        logger.Log(level, 0, null, message, mappedArguments);
        return ValueTask.CompletedTask;
    }
}