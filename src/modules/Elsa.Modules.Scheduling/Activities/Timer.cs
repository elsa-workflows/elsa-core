﻿using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Elsa.Attributes;
using Elsa.Models;
using Elsa.Services;

namespace Elsa.Modules.Scheduling.Activities;

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

    protected override object GetTriggerDatum(TriggerIndexingContext context)
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