using System.Runtime.CompilerServices;
using Elsa.Common;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Framework.System;
using Elsa.Scheduling.Bookmarks;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
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
    [Input]
    public Input<DateTimeOffset> DateTime { get; set; } = default!;

    /// <inheritdoc />
    protected override object GetTriggerPayload(TriggerIndexingContext context)
    {
        var executeAt = context.ExpressionExecutionContext.Get(DateTime);
        return new StartAtPayload(executeAt);
    }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        if (context.IsTriggerOfWorkflow())
        {
            await context.CompleteActivityAsync();
            return;
        }

        var executeAt = context.ExpressionExecutionContext.Get(DateTime);
        var clock = context.ExpressionExecutionContext.GetRequiredService<ISystemClock>();
        var now = clock.UtcNow;
        var logger = context.GetRequiredService<ILogger<StartAt>>();

        context.JournalData.Add("Executed At", now);
        
        if (executeAt <= now)
        {
            logger.LogDebug("Scheduled trigger time lies in the past ('{Delta}'). Completing immediately", now - executeAt);
            await context.CompleteActivityAsync();
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