using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Elsa.Extensions;
using Elsa.Logging.UI;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;
using Microsoft.Extensions.Logging;

namespace Elsa.Logging.Activities;

/// <summary>
/// Emits structured log entries into configured sinks.
/// </summary>
[Activity("Elsa", "Diagnostics", "Emit structured log entries into configured sinks.")]
public class Log : CodeActivity
{
    /// <inheritdoc />
    [JsonConstructor]
    protected Log(string? source = null, int? line = null) : base(source, line)
    {
    }

    /// <inheritdoc />
    public Log(string message, LogLevel level = LogLevel.Information, [CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) 
        : base(source, line)
    {
        Message = new(message);
        Level = new(level);
    }

    /// <inheritdoc />
    public Log(Input<string> message, Input<LogLevel> level, [CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) 
        : base(source, line)
    {
        Message = message;
        Level = level;
    }

    /// <summary>
    /// The log message.
    /// </summary>
    [Input(Description = "The log message to emit.")]
    public Input<string> Message { get; set; } = new(string.Empty);

    /// <summary>
    /// The log level.
    /// </summary>
    [Input(Description = "The log level (Trace, Debug, Information, Warning, Error, Critical).")]
    public Input<LogLevel> Level { get; set; } = new(LogLevel.Information);

    /// <summary>
    /// Additional attributes to include in the log entry.
    /// </summary>
    [Input(Description = "Flat dictionary of key/value pairs to include as attributes.")] 
    public Input<ICollection<object?>> Arguments { get; set; } = null!;

    /// <summary>
    /// The event ID to associate with the log entry. Can be used to correlate logs with specific events.
    /// </summary>
    [Input(Description = "The event ID to associate with the log entry. Can be used to correlate logs with specific events.")]
    public Input<EventId?> EventId { get; set; } = null!;

    /// <summary>
    /// Log category. Defaults to "Process".
    /// </summary>
    [Input(
        Description = "Target sinks. Defaults to 'Process' if not specified.",
        UIHint = InputUIHints.CheckList,
        UIHandler = typeof(LogSinkCheckListUIHintHandler)
    )]
    public Input<ICollection<string>> SinkNames { get; set; } = new(["Process"]);

    /// <inheritdoc />
    protected override void Execute(ActivityExecutionContext context)
    {
        var message = Message.Get(context);
        var level = Level.Get(context);
        var arguments = (Arguments.GetOrDefault(context) ?? new List<object?>()).ToArray(); 
        var eventId = EventId.GetOrDefault(context) ?? default(EventId);
        var targets = SinkNames.GetOrDefault(context) ?? new List<string> { "Process" };
        var loggerFactory = context.GetRequiredService<ILoggerFactory>();

        foreach (var target in targets)
        {
            var categoryName = "Elsa.Log." + target;
            var logger = loggerFactory.CreateLogger(categoryName);
            if(logger.IsEnabled(level))
            {
                logger.Log(level, eventId, null, message, arguments);
            }
        }
    }
}