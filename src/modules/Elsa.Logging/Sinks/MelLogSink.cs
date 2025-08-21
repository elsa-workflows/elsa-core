using Elsa.Logging.Contracts;
using Microsoft.Extensions.Logging;

namespace Elsa.Logging.Sinks;

/// <summary>
/// A Microsoft.Extensions.Logging (MEL) sink, built from a private LoggerFactory configured with any providers.
/// </summary>
public sealed class MelLogSink(string name, ILoggerFactory factory, string category) : ILogSink
{
    public string Name { get; } = name;
    private readonly ILogger _logger = factory.CreateLogger(category);

    public ValueTask WriteAsync(LogLevel level, string message, IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
    {
        using var scope = properties is null ? null : _logger.BeginScope(properties);
        _logger.Log(level, new(0, Name), message, null, (s, e) => s!);
        return ValueTask.CompletedTask;
    }
}