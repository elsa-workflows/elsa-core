using System;
using Elsa.Attributes;
using Elsa.Contracts;
using Elsa.Models;

namespace Elsa.Modules.Scheduling.Activities;

[Activity("Scheduling", "Delays execution for the specified amount of time.")]
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

        context.CreateBookmark(payload);
    }
}

public record DelayPayload(DateTimeOffset ResumeAt);