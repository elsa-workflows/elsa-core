using System;
using Elsa.Common.Services;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Scheduling.Activities;

[Activity( "Elsa", "Scheduling", "Delay execution for the specified amount of time.")]
public class Delay : Activity
{
    public Delay()
    {
    }

    public Delay(Input<TimeSpan> timeSpan) => TimeSpan = timeSpan;
    public Delay(TimeSpan timeSpan) => TimeSpan = new Input<TimeSpan>(timeSpan);
    public Delay(Variable<TimeSpan> timeSpan) => TimeSpan = new Input<TimeSpan>(timeSpan);

    [Input] public Input<TimeSpan> TimeSpan { get; set; } = default!;

    protected override void Execute(ActivityExecutionContext context)
    {
        var timeSpan = context.ExpressionExecutionContext.Get(TimeSpan);
        var clock = context.ExpressionExecutionContext.GetRequiredService<ISystemClock>();
        var resumeAt = clock.UtcNow.Add(timeSpan);
        var payload = new DelayPayload(resumeAt);

        context.JournalData.Add("ResumeAt", resumeAt);
        context.CreateBookmark(payload);
    }

    public static Delay FromMilliseconds(double value) => new(System.TimeSpan.FromMilliseconds(value));
    public static Delay FromSeconds(double value) => new(System.TimeSpan.FromSeconds(value));
    public static Delay FromMinutes(double value) => new(System.TimeSpan.FromMinutes(value));
    public static Delay FromHours(double value) => new(System.TimeSpan.FromHours(value));
    public static Delay FromDays(double value) => new(System.TimeSpan.FromDays(value));
}

public record DelayPayload(DateTimeOffset ResumeAt);