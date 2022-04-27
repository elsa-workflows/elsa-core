using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Models;
using Elsa.Services;
using Microsoft.Extensions.Logging;

namespace Elsa.Modules.Scheduling.Activities;

[Activity("Elsa", "Scheduling", "Trigger execution at a specific time in the future.")]
public class StartAt : Trigger
{
    public StartAt()
    {
    }

    public StartAt(Input<DateTimeOffset> dateTime) => DateTime = dateTime;

    public StartAt(Func<ExpressionExecutionContext, DateTimeOffset> dateTime) : this(new Input<DateTimeOffset>(dateTime))
    {
    }
    
    public StartAt(Func<ExpressionExecutionContext, ValueTask<DateTimeOffset>> dateTime) : this(new Input<DateTimeOffset>(dateTime))
    {
    }
    
    public StartAt(Func<ValueTask<DateTimeOffset>> dateTime) : this(new Input<DateTimeOffset>(dateTime))
    {
    }
    
    public StartAt(Func<DateTimeOffset> dateTime) : this(new Input<DateTimeOffset>(dateTime))
    {
    }

    public StartAt(DateTimeOffset dateTime) => DateTime = new Input<DateTimeOffset>(dateTime);
    public StartAt(Variable<DateTimeOffset> dateTime) => DateTime = new Input<DateTimeOffset>(dateTime);

    [Input] public Input<DateTimeOffset> DateTime { get; set; } = default!;

    protected override object GetTriggerDatum(TriggerIndexingContext context)
    {
        var executeAt = context.ExpressionExecutionContext.Get(DateTime);
        return new StartAtPayload(executeAt);
    }

    protected override void Execute(ActivityExecutionContext context)
    {
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

    public static StartAt From(DateTimeOffset value) => new(value);
}

public record StartAtPayload(DateTimeOffset ExecuteAt);