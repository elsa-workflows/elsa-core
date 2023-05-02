using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Elsa.Common.Contracts;
using Elsa.Extensions;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;

namespace Elsa.Scheduling.Activities;

/// <summary>
/// Represents a timer to periodically trigger the workflow.
/// </summary>
[Activity("Elsa", "Scheduling", "Trigger workflow execution at a specific interval.")]
public class Timer : EventGenerator
{
    /// <inheritdoc />
    [JsonConstructor]
    public Timer([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    /// <inheritdoc />
    public Timer(TimeSpan interval, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(new Input<TimeSpan>(interval), source, line)
    {
    }

    /// <inheritdoc />
    public Timer(Input<TimeSpan> interval, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(source, line)
    {
        Interval = interval;
    }

    /// <summary>
    /// The interval at which the timer should execute.
    /// </summary>
    [Input(Description = "The interval at which the timer should execute.")]
    public Input<TimeSpan> Interval { get; set; } = default!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        if(context.IsTriggerOfWorkflow())
        {
            await context.CompleteActivityAsync();
            return;
        }
        
        var clock = context.ExpressionExecutionContext.GetRequiredService<ISystemClock>();
        var timeSpan = context.ExpressionExecutionContext.Get(Interval);
        var resumeAt = clock.UtcNow.Add(timeSpan);
        
        context.JournalData.Add("ResumeAt", resumeAt);
        context.CreateBookmark(new TimerBookmarkPayload(resumeAt));
    }

    /// <inheritdoc />
    protected override object GetTriggerPayload(TriggerIndexingContext context)
    {
        var interval = context.ExpressionExecutionContext.Get(Interval);
        var clock = context.ExpressionExecutionContext.GetRequiredService<ISystemClock>();
        var executeAt = clock.UtcNow.Add(interval);
        return new TimerTriggerPayload(executeAt, interval);
    }

    /// <summary>
    /// Creates a new <see cref="Timer"/> activity set to trigger at the specified interval.
    /// </summary>
    public static Timer FromTimeSpan(TimeSpan value, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) => new(value, source, line);

    /// <summary>
    /// Creates a new <see cref="Timer"/> activity set to trigger at the specified interval in seconds.
    /// </summary>
    public static Timer FromSeconds(double value, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) => FromTimeSpan(TimeSpan.FromSeconds(value), source, line);
}

internal record TimerTriggerPayload(DateTimeOffset StartAt, TimeSpan Interval);
internal record TimerBookmarkPayload(DateTimeOffset ResumeAt);