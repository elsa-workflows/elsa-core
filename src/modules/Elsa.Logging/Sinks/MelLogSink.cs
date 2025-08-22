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

    public ValueTask WriteAsync(LogLevel level, string message, object? arguments, IDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
    {
        if (!_logger.IsEnabled(level))
            return ValueTask.CompletedTask;

        using var scope = properties is null ? null : _logger.BeginScope(properties);
        _logger.Log(level, 0, arguments, null, (state, ex) => FormatMessage(message, state));
        return ValueTask.CompletedTask;
    }

    private static string FormatMessage(string message, object? state)
    {
        // If the state is an array, use string.Format.
        if (state is object[] { Length: > 0 } args)
            return string.Format(message, args);

        // Otherwise, use string interpolation. No need to use StringBuilder here, since the message is not expected to be long.
        var formattedMessage = message;

        // If the state is a dictionary, use string interpolation.
        if (state is IDictionary<string, object?> dict)
        {
            foreach (var kvp in dict)
                formattedMessage = formattedMessage.Replace($"{{{kvp.Key}}}", kvp.Value?.ToString());
        }
        // Otherwise, use reflection to find properties on the state object.
        else if (state is not null)
        {
            var props = state.GetType().GetProperties();
            foreach (var prop in props)
            {
                var value = prop.GetValue(state);
                formattedMessage = formattedMessage.Replace($"{{{prop.Name}}}", value?.ToString());
            }
        }

        return formattedMessage;
    }
}