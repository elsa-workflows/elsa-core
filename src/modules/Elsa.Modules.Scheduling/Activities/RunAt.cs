using System;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Models;
using Elsa.Services;
using Microsoft.Extensions.Logging;

namespace Elsa.Modules.Scheduling.Activities;

[Activity("Elsa", "Scheduling", "Delay execution for the specified amount of time.")]
public class RunAt : Activity
{
    public RunAt()
    {
    }

    public RunAt(Input<DateTimeOffset> dateTime) => DateTime = dateTime;

    public RunAt(Func<ExpressionExecutionContext, DateTimeOffset> dateTime) : this(new Input<DateTimeOffset>(dateTime))
    {
    }
    
    public RunAt(Func<ExpressionExecutionContext, ValueTask<DateTimeOffset>> dateTime) : this(new Input<DateTimeOffset>(dateTime))
    {
    }
    
    public RunAt(Func<ValueTask<DateTimeOffset>> dateTime) : this(new Input<DateTimeOffset>(dateTime))
    {
    }
    
    public RunAt(Func<DateTimeOffset> dateTime) : this(new Input<DateTimeOffset>(dateTime))
    {
    }

    public RunAt(DateTimeOffset dateTime) => DateTime = new Input<DateTimeOffset>(dateTime);
    public RunAt(Variable<DateTimeOffset> dateTime) => DateTime = new Input<DateTimeOffset>(dateTime);

    [Input] public Input<DateTimeOffset> DateTime { get; set; } = default!;

    protected override void Execute(ActivityExecutionContext context)
    {
        // TODO: Update e.g. ScheduleWorkflows and other places to make sure this activity works correctly when suspending & resuming workflows.  
        
        var executeAt = context.ExpressionExecutionContext.Get(DateTime);
        var clock = context.ExpressionExecutionContext.GetRequiredService<ISystemClock>();
        var now = clock.UtcNow;
        var logger = context.GetRequiredService<ILogger<RunAt>>();

        if (executeAt <= now)
        {
            logger.LogDebug("Scheduled trigger time lies in the past ('{Delta}'). Skipping scheduling", now - executeAt);
            context.JournalData.Add("Executed At", now);
            return;
        }

        var payload = new RunAtPayload(executeAt);

        context.CreateBookmark(payload);
    }

    public static RunAt From(DateTimeOffset value) => new(value);
}

public record RunAtPayload(DateTimeOffset ResumeAt);