using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Elsa.Extensions;
using Elsa.Logging.Contracts;
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
    [Input(Description = "The log message to emit.")] public Input<string> Message { get; set; } = new(string.Empty);

    /// <summary>
    /// The log level.
    /// </summary>
    [Input(Description = "The log level (Trace, Debug, Information, Warning, Error, Critical).")] public Input<LogLevel> Level { get; set; } = new(LogLevel.Information);

    /// <summary>
    /// The log message.
    /// </summary>
    [Input(Description = "The category. Defaults to 'Workflow'.", DefaultValue = "Workflow")] public Input<string> Category { get; set; } = new("Workflow");

    /// <summary>
    /// Additional attributes to include in the log entry.
    /// </summary>
    [Input(Description = "Values of placeholders in the log message.")] public Input<ICollection<object?>> Arguments { get; set; } = null!;

    /// <summary>
    /// Additional attributes to include in the log entry.
    /// </summary>
    [Input(Description = "Flat dictionary of key/value pairs to include as attributes.")] public Input<IDictionary<string, object?>> Attributes { get; set; } = null!;

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
        var cancellationToken = context.CancellationToken;
        var message = Message.Get(context);
        var level = Level.Get(context);
        var arguments = (Arguments.GetOrDefault(context) ?? new List<object?>()).ToArray();
        var properties = Attributes.GetOrDefault(context) ?? new Dictionary<string, object?>();
        var sinkNames = SinkNames.GetOrDefault(context) ?? new List<string>();
        //var catalog = context.GetRequiredService<ILogSinkCatalog>();
        var router = context.GetRequiredService<ILogSinkRouter>();
        var category = Category.GetOrDefault(context);
        if (string.IsNullOrWhiteSpace(category)) category = "Workflow";

        properties["WorkflowDefinitionId"] = context.WorkflowExecutionContext.Workflow.Identity.DefinitionId;
        properties["WorkflowDefinitionVersionId"] = context.WorkflowExecutionContext.Workflow.Identity.Id;
        properties["WorkflowDefinitionVersion"] = context.WorkflowExecutionContext.Workflow.Identity.Version;
        properties["WorkflowInstanceId"] = context.WorkflowExecutionContext.Id;

        await router.WriteAsync(sinkNames, level, message, arguments, properties, cancellationToken);
    }
}