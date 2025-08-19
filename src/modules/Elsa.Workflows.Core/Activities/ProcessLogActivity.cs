using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Elsa.Common;
using Elsa.Extensions;
using Elsa.Expressions.Models;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Activities;

/// <summary>
/// Emits structured log entries into configured sinks.
/// </summary>
[Activity("Elsa", "Logging", "Emit structured log entries into configured sinks.")]
public class ProcessLogActivity : CodeActivity
{
    /// <inheritdoc />
    [JsonConstructor]
    private ProcessLogActivity(string? source = null, int? line = null) : base(source, line)
    {
    }

    /// <inheritdoc />
    public ProcessLogActivity(string message, Models.LogLevel level = Models.LogLevel.Information, [CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) 
        : base(source, line)
    {
        Message = new Input<string>(message);
        Level = new Input<Models.LogLevel>(level);
    }

    /// <inheritdoc />
    public ProcessLogActivity(Input<string> message, Input<Models.LogLevel> level, [CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) 
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
    public Input<Models.LogLevel> Level { get; set; } = new(Models.LogLevel.Information);

    /// <summary>
    /// The target sink names. If not specified, defaults to "process".
    /// </summary>
    [Input(Description = "Optional list of logical sink names. Defaults to 'process' if not specified.")]
    public Input<IList<string>?> TargetSinks { get; set; } = new(default(IList<string>));

    /// <summary>
    /// Additional attributes to include in the log entry.
    /// </summary>
    [Input(Description = "Flat dictionary of key/value pairs to include as attributes.")]
    public Input<IDictionary<string, object>?> Attributes { get; set; } = new(default(IDictionary<string, object>));

    /// <summary>
    /// Optional event ID.
    /// </summary>
    [Input(Description = "Optional event ID for the log entry.")]
    public Input<int?> EventId { get; set; } = new(default(int?));

    /// <summary>
    /// Log category. Defaults to "Process".
    /// </summary>
    [Input(Description = "Log category. Defaults to 'Process' if not specified.")]
    public Input<string?> Category { get; set; } = new(default(string));

    /// <inheritdoc />
    protected override void Execute(ActivityExecutionContext context)
    {
        var message = context.Get(Message);
        var level = context.Get(Level);
        var targetSinks = context.Get(TargetSinks) ?? new List<string> { "process" };
        var attributes = context.Get(Attributes) ?? new Dictionary<string, object>();
        var eventId = context.Get(EventId);
        var category = context.Get(Category) ?? "Process";

        // Get services
        var logSinkResolver = context.GetService<ILogSinkResolver>();
        var logger = context.GetService<ILogger<ProcessLogActivity>>();
        var systemClock = context.GetService<ISystemClock>();

        if (logSinkResolver == null)
        {
            logger?.LogWarning("No ILogSinkResolver service available. ProcessLogActivity cannot emit logs.");
            return;
        }

        // Create log entry with workflow context enrichment
        var workflowContext = context.WorkflowExecutionContext;
        var logEntry = new ProcessLogEntry(
            Message: message ?? string.Empty,
            Level: level,
            TargetSinks: targetSinks.ToList().AsReadOnly(),
            Attributes: attributes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value).AsReadOnly(),
            EventId: eventId,
            Category: category,
            Timestamp: systemClock?.UtcNow ?? DateTimeOffset.UtcNow,
            WorkflowInstanceId: workflowContext.Id,
            WorkflowName: workflowContext.Workflow.Name,
            ActivityId: context.Activity.Id,
            ActivityName: context.Activity.Name,
            CorrelationId: workflowContext.CorrelationId
        );

        // Resolve sinks and emit log entry
        var sinks = logSinkResolver.Resolve(targetSinks).ToList();

        if (!sinks.Any())
        {
            logger?.LogWarning("No sinks resolved for target sinks: {TargetSinks}. Check your sink configuration.", string.Join(", ", targetSinks));
            return;
        }

        // Emit to all sinks in parallel (fire-and-forget)
        var tasks = sinks.Select(async sink =>
        {
            try
            {
                if (sink.IsEnabled(level, category))
                {
                    await sink.WriteAsync(logEntry, context.CancellationToken);
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't block workflow execution
                logger?.LogWarning(ex, "Failed to write log entry to sink '{SinkName}'. Error: {Error}", sink.Name, ex.Message);
            }
        });

        // Fire and forget to avoid blocking workflow execution
        _ = Task.Run(async () => await Task.WhenAll(tasks), context.CancellationToken);
    }
}