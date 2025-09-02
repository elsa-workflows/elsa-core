using System.Dynamic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Extensions;
using Elsa.Logging.Contracts;
using Elsa.Logging.Models;
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
    /// Additional attributes to include in the log entry.
    /// </summary>
    [Input(Description = "Values of named or indexed placeholders in the log message.")] 
    public Input<object?> Arguments { get; set; } = null!;

    /// <summary>
    /// The log level.
    /// </summary>
    [Input(Description = "The log level (Trace, Debug, Information, Warning, Error, Critical).")]
    public Input<LogLevel> Level { get; set; } = new(LogLevel.Information);

    /// <summary>
    /// The log message.
    /// </summary>
    [Input(Description = "The category. Defaults to 'Process'.", DefaultValue = "Process")]
    public Input<string> Category { get; set; } = new("Process");

    /// <summary>
    /// Additional attributes to include in the log entry.
    /// </summary>
    [Input(Description = "Flat dictionary of key/value pairs to include as attributes.")] 
    public Input<IDictionary<string, object?>> Attributes { get; set; } = null!;

    /// <summary>
    /// Target sinks to write to.
    /// </summary>
    [Input(
        DisplayName = "Sinks",
        Description = "Target sinks to write to.",
        UIHint = InputUIHints.CheckList,
        UIHandler = typeof(LogSinkCheckListUIHintHandler)
    )]
    public Input<ICollection<string>> SinkNames { get; set; } = null!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var message = Message.Get(context);
        var level = Level.Get(context);
        var arguments = Arguments.GetOrDefault(context);

        if (arguments is string argumentString)
        {
            // Could be JSON created from e.g., Liquid template. If so, parse it into an ExpandoObject.
            arguments = TryParseJson(argumentString);
        }
        
        var attributes = Attributes.GetOrDefault(context) ?? new Dictionary<string, object?>();
        var sinkNames = SinkNames.GetOrDefault(context) ?? new List<string>();
        var category = Category.GetOrDefault(context);
        if (string.IsNullOrWhiteSpace(category)) category = "Process";

        attributes["WorkflowDefinitionId"] = context.WorkflowExecutionContext.Workflow.Identity.DefinitionId;
        attributes["WorkflowDefinitionVersionId"] = context.WorkflowExecutionContext.Workflow.Identity.Id;
        attributes["WorkflowDefinitionVersion"] = context.WorkflowExecutionContext.Workflow.Identity.Version;
        attributes["WorkflowInstanceId"] = context.WorkflowExecutionContext.Id;

        var queue = context.GetRequiredService<ILogEntryQueue>();
        var instruction = new LogEntryInstruction
        {
            SinkNames = sinkNames,
            Category = category,
            Level = level,
            Message = message,
            Arguments = arguments,
            Attributes = attributes
        };
        await queue.EnqueueAsync(instruction);
    }

    private object TryParseJson(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<ExpandoObject>(json) ?? new ExpandoObject();
        }
        catch
        {
            return json;
        }
    }
}