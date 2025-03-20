using Elsa.Common;
using Elsa.Scheduling;
using Elsa.Scheduling.Bookmarks;
using Elsa.Workflows;
using Elsa.Workflows.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class ActivityExecutionContextExtensions
{
    /// <summary>
    /// Resumes the activity after a specified delay.
    /// </summary>
    /// <param name="context">The activity execution context in which the workflow is running.</param>
    /// <param name="delay">The delay before the workflow resumes.</param>
    /// <param name="callback">The delegate to execute when the activity resumes.</param>
    public static void ResumeIn(this ActivityExecutionContext context, TimeSpan delay, ExecuteActivityDelegate callback)
    {
        var clock = context.ExpressionExecutionContext.GetRequiredService<ISystemClock>();
        var resumeAt = clock.UtcNow.Add(delay);
        
        ResumeAt(context, resumeAt, callback);
    }

    /// <summary>
    /// Resumes the activity at a specified point in time.
    /// </summary>
    /// <param name="context">The activity execution context in which the workflow is running.</param>
    /// <param name="resumeAt">The point in time at which the workflow should resume execution.</param>
    /// <param name="callback">The delegate to execute when the activity resumes.</param>
    public static void ResumeAt(this ActivityExecutionContext context, DateTimeOffset resumeAt, ExecuteActivityDelegate callback)
    {
        var payload = new DelayPayload(resumeAt);

        var bookmarkOptions = new CreateBookmarkArgs
        {
            BookmarkName = SchedulingStimulusNames.Delay,
            Stimulus = payload,
            Callback = callback
        };
        context.CreateBookmark(bookmarkOptions);
    }
}