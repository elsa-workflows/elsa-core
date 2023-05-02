using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Elsa.Common.Contracts;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Microsoft.Extensions.Logging;

namespace Elsa.Scheduling.Activities;

/// <summary>
/// Triggers the workflow at a specific future timestamp.
/// </summary>
[Activity("Elsa", "Scheduling", "Trigger execution at a specific time in the future.")]
public class StartAt : Trigger
{
    private const string InputKey = "ExecuteAt";

    /// <inheritdoc />
    [JsonConstructor]
    public StartAt([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    /// <inheritdoc />
    public StartAt(Input<DateTimeOffset> dateTime, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(source, line) => DateTime = dateTime;

    /// <inheritdoc />
    public StartAt(Func<ExpressionExecutionContext, DateTimeOffset> dateTime, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) 
        : this(new Input<DateTimeOffset>(dateTime), source, line)
    {
    }

    /// <inheritdoc />
    public StartAt(
        Func<ExpressionExecutionContext, ValueTask<DateTimeOffset>> dateTime,
        [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(new Input<DateTimeOffset>(dateTime), source, line)
    {
    }

    /// <inheritdoc />
    public StartAt(Func<ValueTask<DateTimeOffset>> dateTime, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) 
        : this(new Input<DateTimeOffset>(dateTime), source, line)
    {
    }

    /// <inheritdoc />
    public StartAt(Func<DateTimeOffset> dateTime, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) 
        : this(new Input<DateTimeOffset>(dateTime), source, line)
    {
    }

    /// <inheritdoc />
    public StartAt(DateTimeOffset dateTime, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(source, line) => 
        DateTime = new Input<DateTimeOffset>(dateTime);

    /// <inheritdoc />
    public StartAt(Variable<DateTimeOffset> dateTime, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(source, line) => 
        DateTime = new Input<DateTimeOffset>(dateTime);

    /// <summary>
    /// The timestamp at which the workflow should be triggered.
    /// </summary>
    [Input] public Input<DateTimeOffset> DateTime { get; set; } = default!;

    /// <inheritdoc />
    protected override object GetTriggerPayload(TriggerIndexingContext context)
    {
        var executeAt = context.ExpressionExecutionContext.Get(DateTime);
        return new StartAtPayload(executeAt);
    }

    /// <inheritdoc />
    protected override void Execute(ActivityExecutionContext context)
    {
        // If external input was received, it means this activity got triggered and does not need to create a bookmark.
        if (context.TryGetInput<DateTimeOffset>(InputKey, out _)) 
            return;
        
        // No external input received, so create a bookmark.
        var executeAt = context.ExpressionExecutionContext.Get(DateTime);
        var clock = context.ExpressionExecutionContext.GetRequiredService<ISystemClock>();
        var now = clock.UtcNow;
        var logger = context.GetRequiredService<ILogger<StartAt>>();

        if (executeAt <= now)
        {
            logger.LogDebug("Scheduled trigger time lies in the past ('{Delta}'). Skipping scheduling", now - executeAt);
            context.JournalData.Add("Executed At", now);
            return;
        }

        var payload = new StartAtPayload(executeAt);

        context.CreateBookmark(payload);
    }

    /// <summary>
    /// Creates a new <see cref="StartAt"/> activity set to trigger at the specified timestamp.
    /// </summary>
    public static StartAt From(DateTimeOffset value, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) => new(value, source, line);
}

internal record StartAtPayload(DateTimeOffset ExecuteAt);