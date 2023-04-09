using Elsa.Extensions;
using Elsa.Workflows.Core.Middleware.Activities;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Pipelines.ActivityExecution;
using Elsa.Workflows.Runtime.Middleware.Workflows;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Models.Bookmarks;

namespace Elsa.Workflows.Runtime.Middleware.Activities;

/// <summary>
/// Executes the current activity from a background job if the activity is of kind <see cref="ActivityKind.Job"/> or <see cref="ActivityKind.Task"/>.
/// Works in tandem with <see cref="ScheduleBackgroundActivitiesMiddleware"/>. 
/// </summary>
public class BackgroundActivityInvokerMiddleware : DefaultActivityInvokerMiddleware
{
    internal static readonly object IsBackgroundExecution = new();
    internal static string GetBackgroundActivityOutputKey(string activityId) => $"__BackgroundActivityOutput:{activityId}";

    /// <inheritdoc />
    public BackgroundActivityInvokerMiddleware(ActivityMiddlewareDelegate next) : base(next)
    {
    }

    /// <inheritdoc />
    protected override async ValueTask ExecuteActivityAsync(ActivityExecutionContext context)
    {
        var shouldRunInBackground = GetShouldRunInBackground(context);

        if (shouldRunInBackground)
            ScheduleBackgroundActivity(context);
        else
        {
            CaptureOutputIfAny(context);
            await base.ExecuteActivityAsync(context);
        }
    }

    /// <summary>
    /// Determines whether the current activity should be executed in the background.
    /// </summary>
    private static bool GetShouldRunInBackground(ActivityExecutionContext context)
    {
        var activity = context.Activity;
        var activityDescriptor = context.ActivityDescriptor;
        var kind = activityDescriptor.Kind;

        return !context.TransientProperties.ContainsKey(IsBackgroundExecution)
               && context.WorkflowExecutionContext.ExecuteDelegate == null
               && (kind is ActivityKind.Job || (kind == ActivityKind.Task && activity.RunAsynchronously));
    }

    /// <summary>
    /// Schedules the current activity for execution in the background.
    /// </summary>
    private static void ScheduleBackgroundActivity(ActivityExecutionContext context)
    {
        var scheduledBackgroundActivities = context.WorkflowExecutionContext.TransientProperties.GetOrAdd(ScheduleBackgroundActivitiesMiddleware.BackgroundActivitySchedulesKey, () => new List<ScheduledBackgroundActivity>());
        var workflowInstanceId = context.WorkflowExecutionContext.Id;
        var activityId = context.Activity.Id;
        var bookmarkPayload = new BackgroundActivityBookmark();
        var bookmark = context.CreateBookmark(bookmarkPayload);
        scheduledBackgroundActivities.Add(new ScheduledBackgroundActivity(workflowInstanceId, activityId, bookmark.Id));
    }

    /// <summary>
    /// If the input contains captured output from the background activity invoker, apply that to the execution context.
    /// </summary>
    /// <param name="context"></param>
    private static void CaptureOutputIfAny(ActivityExecutionContext context)
    {
        var activity = context.Activity;
        var inputKey = GetBackgroundActivityOutputKey(activity.Id);

        if (!context.Input.TryGetValue(inputKey, out var capturedOutput))
            return;

        var input = (IDictionary<string, object>)capturedOutput;
        foreach (var inputEntry in input)
        {
            var outputDescriptor = context.ActivityDescriptor.Outputs.FirstOrDefault(x => x.Name == inputEntry.Key);

            if (outputDescriptor == null)
                continue;

            var output = (Output?)outputDescriptor.ValueGetter(activity);
            context.Set(output, inputEntry.Value);
        }
    }
}