using Elsa.Common;
using Elsa.Scheduling;
using Elsa.Scheduling.Bookmarks;
using Elsa.Workflows;
using Elsa.Workflows.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class TimerActivityExecutionContextExtensions
{
    /// <summary>
    /// Repeats the execution of the current activity at the specified interval.
    /// </summary>
    /// <param name="context">The execution context of the activity.</param>
    /// <param name="interval">The time interval after which the activity should be repeated.</param>
    /// <param name="callback">An optional callback to execute immediately if the workflow is triggered.</param>
    public static void RepeatWithInterval(this ActivityExecutionContext context, TimeSpan interval, ExecuteActivityDelegate? callback = null)
    {
        if(context.IsTriggerOfWorkflow())
        {
            callback?.Invoke(context);
            return;
        }
        
        var clock = context.ExpressionExecutionContext.GetRequiredService<ISystemClock>();
        var resumeAt = clock.UtcNow.Add(interval);

        var bookmarkOptions = new CreateBookmarkArgs
        {
            BookmarkName = SchedulingStimulusNames.Timer,
            Stimulus = new TimerBookmarkPayload(resumeAt),
            Callback = callback
        };
        context.CreateBookmark(bookmarkOptions);
    }
    
    public static TimerTriggerPayload GetTimerTriggerStimulus(this TriggerIndexingContext context, TimeSpan interval)
    {
        var clock = context.ExpressionExecutionContext.GetRequiredService<ISystemClock>();
        var executeAt = clock.UtcNow.Add(interval);
        context.TriggerName = SchedulingStimulusNames.Timer;
        return new(executeAt, interval);
    }
}