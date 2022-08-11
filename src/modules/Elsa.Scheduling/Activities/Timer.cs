using System;
using System.Text.Json.Serialization;
using Elsa.Common.Services;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Scheduling.Activities;

/// <summary>
/// Represents a timer to periodically trigger the workflow.
/// </summary>
[Activity( "Elsa", "Scheduling", "Trigger workflow execution at a specific interval.")] 
public class Timer : EventGenerator
{
    [JsonConstructor]
    public Timer()
    {
    }

    public Timer(TimeSpan interval) : this(new Input<TimeSpan>(interval))
    {
    }

    public Timer(Input<TimeSpan> interval)
    {
        Interval = interval;
    }
    
    [Input] public Input<TimeSpan> Interval { get; set; } = default!;

    protected override object GetTriggerPayload(TriggerIndexingContext context)
    {
        var interval = context.ExpressionExecutionContext.Get(Interval);
        var clock = context.ExpressionExecutionContext.GetRequiredService<ISystemClock>();
        var executeAt = clock.UtcNow.Add(interval);
        return new TimerPayload(executeAt, interval);
    }

    public static Timer FromTimeSpan(TimeSpan value) => new(value);
    public static Timer FromSeconds(double value) => FromTimeSpan(TimeSpan.FromSeconds(value));
}

public record TimerPayload(DateTimeOffset StartAt, TimeSpan Interval);