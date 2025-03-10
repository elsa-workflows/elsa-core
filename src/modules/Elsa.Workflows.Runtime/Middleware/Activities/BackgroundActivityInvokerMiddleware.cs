using System.Text.Json;
using Elsa.Extensions;
using Elsa.Workflows.CommitStates;
using Elsa.Workflows.Middleware.Activities;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.Pipelines.ActivityExecution;
using Elsa.Workflows.Runtime.Stimuli;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime.Middleware.Activities;

/// <summary>
/// Collects the current activity for scheduling for execution from a background job if the activity is of kind <see cref="ActivityKind.Job"/> or <see cref="Task"/>.
/// </summary>
[UsedImplicitly]
public class BackgroundActivityInvokerMiddleware(
    ActivityMiddlewareDelegate next,
    ILogger<BackgroundActivityInvokerMiddleware> logger,
    IIdentityGenerator identityGenerator,
    IBackgroundActivityScheduler backgroundActivityScheduler,
    ICommitStrategyRegistry commitStrategyRegistry)
    : DefaultActivityInvokerMiddleware(next, commitStrategyRegistry, logger)
{
    internal static string GetBackgroundActivityOutputKey(string activityNodeId) => $"__BackgroundActivityOutput:{activityNodeId}";
    internal static string GetBackgroundActivityOutcomesKey(string activityNodeId) => $"__BackgroundActivityOutcomes:{activityNodeId}";
    internal static string GetBackgroundActivityCompletedKey(string activityNodeId) => $"__BackgroundActivityCompleted:{activityNodeId}";
    internal static string GetBackgroundActivityJournalDataKey(string activityNodeId) => $"__BackgroundActivityJournalData:{activityNodeId}";
    internal static string GetBackgroundActivityScheduledActivitiesKey(string activityNodeId) => $"__BackgroundActivityScheduledActivities:{activityNodeId}";
    internal static string GetBackgroundActivityBookmarksKey(string activityNodeId) => $"__BackgroundActivityBookmarks:{activityNodeId}";
    internal const string BackgroundActivityBookmarkName = "BackgroundActivity";

    /// <inheritdoc />
    protected override async ValueTask ExecuteActivityAsync(ActivityExecutionContext context)
    {
        var shouldRunInBackground = GetShouldRunInBackground(context);

        if (shouldRunInBackground)
            await ScheduleBackgroundActivityAsync(context);
        else
        {
            await base.ExecuteActivityAsync(context);

            // This part is either executed from the background or in the foreground when the activity is resumed.
            var isResuming = !GetIsBackgroundExecution(context) && context.ActivityDescriptor.Kind is ActivityKind.Task or ActivityKind.Job;
            if (isResuming)
            {
                CaptureOutputIfAny(context);
                CaptureJournalData(context);
                CaptureBookmarkData(context);
                await CompleteBackgroundActivityOutcomesAsync(context);
                await CompleteBackgroundActivityAsync(context);
                await CompleteBackgroundActivityScheduledActivitiesAsync(context);
            }
        }
    }

    /// <summary>
    /// Schedules the current activity for execution in the background.
    /// </summary>
    private async Task ScheduleBackgroundActivityAsync(ActivityExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;
        var workflowInstanceId = context.WorkflowExecutionContext.Id;
        var activityNodeId = context.NodeId;
        var bookmarkId = identityGenerator.GenerateId();
        var scheduledBackgroundActivity = new ScheduledBackgroundActivity(workflowInstanceId, activityNodeId, bookmarkId);
        var jobId = await backgroundActivityScheduler.CreateAsync(scheduledBackgroundActivity, cancellationToken);
        var stimulus = new BackgroundActivityStimulus
        {
            JobId = jobId
        };
        var bookmarkOptions = new CreateBookmarkArgs
        {
            BookmarkId = bookmarkId,
            BookmarkName = BackgroundActivityBookmarkName,
            Stimulus = stimulus,
            AutoComplete = false
        };
        context.CreateBookmark(bookmarkOptions);

        context.DeferTask(async () =>
        {
            await backgroundActivityScheduler.ScheduleAsync(jobId, cancellationToken);
        });
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

    private static bool GetIsBackgroundExecution(ActivityExecutionContext context) => context.TransientProperties.ContainsKey(BackgroundActivityExecutionContextExtensions.IsBackgroundExecution);
    
    /// <summary>
    /// If the input contains captured output from the background activity invoker, apply that to the execution context.
    /// </summary>
    private static void CaptureOutputIfAny(ActivityExecutionContext context)
    {
        var activity = context.Activity;
        var inputKey = GetBackgroundActivityOutputKey(activity.NodeId);
        var capturedOutput = context.WorkflowExecutionContext.GetProperty<IDictionary<string, object>>(inputKey);

        context.WorkflowExecutionContext.Properties.Remove(inputKey);

        if (capturedOutput == null)
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

        context.WorkflowExecutionContext.Properties.Remove(journalDataKey);

        if (journalData == null)
            return;

        foreach (var journalEntry in journalData)
            context.JournalData[journalEntry.Key] = journalEntry.Value;
    }

    private void CaptureBookmarkData(ActivityExecutionContext context)
    {
        var activity = context.Activity;
        var bookmarksKey = GetBackgroundActivityBookmarksKey(activity.NodeId);
        var bookmarks = context.WorkflowExecutionContext.GetProperty<ICollection<Bookmark>>(bookmarksKey);
        if (bookmarks != null)
        {
            context.AddBookmarks(bookmarks);
        }

        context.WorkflowExecutionContext.Properties.Remove(bookmarksKey);
    }

    private async Task CompleteBackgroundActivityOutcomesAsync(ActivityExecutionContext context)
    {
        var outcomesKey = GetBackgroundActivityOutcomesKey(context.NodeId);
        var outcomes = context.WorkflowExecutionContext.GetProperty<ICollection<string>>(outcomesKey);

        if (outcomes != null)
        {
            await context.CompleteActivityWithOutcomesAsync(outcomes.ToArray());
        }

        // Remove the outcomes from the workflow execution context.
        context.WorkflowExecutionContext.Properties.Remove(outcomesKey);
    }

    private async Task CompleteBackgroundActivityAsync(ActivityExecutionContext context)
    {
        var completedKey = GetBackgroundActivityCompletedKey(context.NodeId);
        var completed = context.WorkflowExecutionContext.GetProperty<bool?>(completedKey);

        if (completed is true)
        {
            await context.CompleteActivityAsync();
        }

        // Remove the outcomes from the workflow execution context.
        context.WorkflowExecutionContext.Properties.Remove(completedKey);
    }

    private async Task CompleteBackgroundActivityScheduledActivitiesAsync(ActivityExecutionContext context)
    {
        var scheduledActivitiesKey = GetBackgroundActivityScheduledActivitiesKey(context.NodeId);
        var scheduledActivitiesJson = context.WorkflowExecutionContext.GetProperty<string>(scheduledActivitiesKey);
        var scheduledActivities = scheduledActivitiesJson != null ? JsonSerializer.Deserialize<ICollection<ScheduledActivity>>(scheduledActivitiesJson) : null;

        if (scheduledActivities != null)
        {
            foreach (var scheduledActivity in scheduledActivities)
            {
                var activityNode = scheduledActivity.ActivityNodeId != null ? context.WorkflowExecutionContext.FindActivityByNodeId(scheduledActivity.ActivityNodeId) : null;
                var owner = scheduledActivity.OwnerActivityInstanceId != null ? context.WorkflowExecutionContext.ActivityExecutionContexts.FirstOrDefault(x => x.Id == scheduledActivity.OwnerActivityInstanceId) : null;
                var options = scheduledActivity.Options != null
                    ? new ScheduleWorkOptions
                    {
                        ExistingActivityExecutionContext = scheduledActivity.Options.ExistingActivityInstanceId != null ? context.WorkflowExecutionContext.ActivityExecutionContexts.FirstOrDefault(x => x.Id == scheduledActivity.Options.ExistingActivityInstanceId) : null,
                        Variables = scheduledActivity.Options?.Variables,
                        CompletionCallback = !string.IsNullOrEmpty(scheduledActivity.Options?.CompletionCallback) && owner != null ? owner.Activity.GetActivityCompletionCallback(scheduledActivity.Options.CompletionCallback) : default,
                        PreventDuplicateScheduling = scheduledActivity.Options?.PreventDuplicateScheduling ?? false,
                        Input = scheduledActivity.Options?.Input,
                        Tag = scheduledActivity.Options?.Tag
                    }
                    : default;
                await context.ScheduleActivityAsync(activityNode, owner, options);
            }
        }

        // Remove the scheduled activities from the workflow execution context.
        context.WorkflowExecutionContext.Properties.Remove(scheduledActivitiesKey);
    }
}