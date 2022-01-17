using System;
using System.Collections.Generic;
using Elsa.Attributes;
using Elsa.Contracts;
using Elsa.Models;

namespace Elsa.Activities.Scheduling;

/// <summary>
/// Represents a timer to periodically trigger the workflow it is associated with.
/// </summary>
public class Timer : Trigger
{
    [Input] public Input<TimeSpan> Interval { get; set; } = default!;

    protected override IEnumerable<object> GetPayloads(TriggerIndexingContext context)
    {
        var interval = context.ExpressionExecutionContext.Get(Interval);
        var clock = context.ExpressionExecutionContext.GetRequiredService<ISystemClock>();
        var executeAt = clock.UtcNow.Add(interval);
        yield return new TimerPayload(executeAt, interval);
    }
}

public record TimerPayload(DateTime StartAt, TimeSpan Interval);