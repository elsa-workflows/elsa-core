using Elsa.Extensions;
using Elsa.Workflows.Middleware.Activities;
using Elsa.Workflows.Models;
using Elsa.Workflows.Pipelines.ActivityExecution;
using Elsa.Workflows.Runtime.Bookmarks;
using Elsa.Workflows.Runtime.Middleware.Workflows;
using Elsa.Workflows.Runtime.Models;

namespace Elsa.Workflows.Runtime.Middleware.Activities;

/// <summary>
/// Collects the current activity for scheduling for execution from a background job if the activity is of kind <see cref="ActivityKind.Job"/> or <see cref="Task"/>.
/// The actual scheduling of the activity happens in <see cref="ScheduleBackgroundActivitiesMiddleware"/>.
/// </summary>
public class BackgroundActivityInvokerMiddleware : DefaultActivityInvokerMiddleware
{
    /// <summary>
    /// A key into the activity execution context's transient properties that indicates whether the current activity is being executed in the background.
    /// </summary>
    public static readonly object IsBackgroundExecution = new();

    internal static string GetBackgroundActivityOutputKey(string activityNodeId) => $"__BackgroundActivityOutput:{activityNodeId}";
    internal static string GetBackgroundActivityOutcomesKey(string activityNodeId) => $"__BackgroundActivityOutcomes:{activityNodeId}";
    internal static string GetBackgroundActivityJournalDataKey(string activityNodeId) => $"__BackgroundActivityJournalData:{activityNodeId}";
    internal static readonly object BackgroundActivitySchedulesKey = new();
    internal const string BackgroundActivityBookmarkName = "BackgroundActivity";

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
            await base.ExecuteActivityAsync(context);

            // This part is either executed from the background, or in the foreground when the activity is resumed.
            var isResuming = !GetIsBackgroundExecution(context) && context.ActivityDescriptor.Kind is ActivityKind.Task or ActivityKind.Job;
            if (isResuming)
            {
                CaptureOutputIfAny(context);
                CaptureJournalData(context);
                await CompleteBackgroundActivityAsync(context);
            }
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

        return !GetIsBackgroundExecution(context)
               && context.WorkflowExecutionContext.ExecuteDelegate == null
               && (kind is ActivityKind.Job || (kind == ActivityKind.Task && activity.GetRunAsynchronously()));
    }

    private static bool GetIsBackgroundExecution(ActivityExecutionContext context) => context.TransientProperties.ContainsKey(IsBackgroundExecution);

    /// <summary>
    /// Schedules the current activity for execution in the background.
    /// </summary>
    private static void ScheduleBackgroundActivity(ActivityExecutionContext context)
    {
        var scheduledBackgroundActivities = context.WorkflowExecutionContext.TransientProperties.GetOrAdd(BackgroundActivitySchedulesKey, () => new List<ScheduledBackgroundActivity>());
        var workflowInstanceId = context.WorkflowExecutionContext.Id;
        var activityNodeId = context.NodeId;
        var bookmarkPayload = new BackgroundActivityBookmark();
        var bookmarkOptions = new CreateBookmarkArgs { BookmarkName = BackgroundActivityBookmarkName, Payload = bookmarkPayload, AutoComplete = false };
        var bookmark = context.CreateBookmark(bookmarkOptions);
        scheduledBackgroundActivities.Add(new ScheduledBackgroundActivity(workflowInstanceId, activityNodeId, bookmark.Id));
    }

    /// <summary>
    /// If the input contains captured output from the background activity invoker, apply that to the execution context.
    /// </summary>
    /// <param name="context"></param>
    private static void CaptureOutputIfAny(ActivityExecutionContext context)
    {
        var activity = context.Activity;
        var inputKey = GetBackgroundActivityOutputKey(activity.NodeId);
        var capturedOutput = context.WorkflowExecutionContext.GetProperty<IDictionary<string, object>>(inputKey);
        
        if(capturedOutput == null)
            return;
        
        foreach (var outputEntry in capturedOutput)
        {
            var outputDescriptor = context.ActivityDescriptor.Outputs.FirstOrDefault(x => x.Name == outputEntry.Key);

            if (outputDescriptor == null)
                continue;

            var output = (Output?)outputDescriptor.ValueGetter(activity);
            context.Set(output, outputEntry.Value);
        }
    }
    
    private void CaptureJournalData(ActivityExecutionContext context)
    {
        var activity = context.Activity;
        var journalDataKey = GetBackgroundActivityJournalDataKey(activity.NodeId);
        var journalData = context.WorkflowExecutionContext.GetProperty<IDictionary<string, object>>(journalDataKey);

        if (journalData == null)
            return;

        foreach (var journalEntry in journalData)
            context.JournalData[journalEntry.Key] = journalEntry.Value;
    }
    
    private async Task CompleteBackgroundActivityAsync(ActivityExecutionContext context)
    {
        var outcomesKey = GetBackgroundActivityOutcomesKey(context.NodeId);
        var outcomes = context.WorkflowExecutionContext.GetProperty<ICollection<string>>(outcomesKey);

        if (outcomes != null)
        {
            await context.CompleteActivityWithOutcomesAsync(outcomes.ToArray());
                    
            // Remove the outcomes from the workflow execution context.
            context.WorkflowExecutionContext.Properties.Remove(outcomesKey);
        }
    }
}