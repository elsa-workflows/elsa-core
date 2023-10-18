using Elsa.Common.Contracts;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Options;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Adds extension methods to <see cref="ActivityExecutionContext"/>.
/// </summary>
public static class WorkflowExecutionContextExtensions
{
    /// <summary>
    /// Schedules the workflow for execution.
    /// </summary>
    public static ActivityWorkItem ScheduleWorkflow(this WorkflowExecutionContext workflowExecutionContext, IDictionary<string, object>? input = default)
    {
        var workflow = workflowExecutionContext.Workflow;
        var workItem = new ActivityWorkItem(workflow, input: input);
        workflowExecutionContext.Scheduler.Schedule(workItem);
        return workItem;
    }

    /// <summary>
    /// Schedules the root activity of the workflow.
    /// </summary>
    public static ActivityWorkItem ScheduleRoot(this WorkflowExecutionContext workflowExecutionContext, IDictionary<string, object>? input = default)
    {
        var workflow = workflowExecutionContext.Workflow;
        var workItem = new ActivityWorkItem(workflow.Root, input: input);
        workflowExecutionContext.Scheduler.Schedule(workItem);
        return workItem;
    }

    /// <summary>
    /// Schedules the specified activity of the workflow.
    /// </summary>
    public static ActivityWorkItem ScheduleActivity(this WorkflowExecutionContext workflowExecutionContext, IActivity activity, IDictionary<string, object>? input = default)
    {
        var workItem = new ActivityWorkItem(activity, input: input);
        workflowExecutionContext.Scheduler.Schedule(workItem);
        return workItem;
    }

    /// <summary>
    /// Schedules the specified activity execution context of the workflow.
    /// </summary>
    public static ActivityWorkItem ScheduleActivityExecutionContext(this WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext, IDictionary<string, object>? input = default)
    {
        var workItem = new ActivityWorkItem(activityExecutionContext.Activity, input: input, existingActivityExecutionContext: activityExecutionContext);
        workflowExecutionContext.Scheduler.Schedule(workItem);
        return workItem;
    }

    /// <summary>
    /// Schedules the activity of the specified bookmark.
    /// </summary>
    /// <returns>The created work item, or <c>null</c> if the specified bookmark doesn't exist in the <see cref="WorkflowExecutionContext"/></returns> 
    public static ActivityWorkItem? ScheduleBookmark(this WorkflowExecutionContext workflowExecutionContext, Bookmark bookmark, IDictionary<string, object>? input = default)
    {
        // Get the activity execution context that owns the bookmark.
        var bookmarkedActivityContext = workflowExecutionContext.ActivityExecutionContexts.FirstOrDefault(x => x.Id == bookmark.ActivityInstanceId);

        if (bookmarkedActivityContext == null)
            return null;

        var bookmarkedActivity = bookmarkedActivityContext.Activity;

        // Schedule the activity to resume.
        var workItem = new ActivityWorkItem(bookmarkedActivity)
        {
            ExistingActivityExecutionContext = bookmarkedActivityContext,
            Input = input ?? new Dictionary<string, object>()
        };
        workflowExecutionContext.Scheduler.Schedule(workItem);

        // If no resumption point was specified, use "Complete" to prevent the regular "ExecuteAsync" method to be invoked and instead complete the activity.
        workflowExecutionContext.ExecuteDelegate = bookmark.CallbackMethodName != null ? bookmarkedActivity.GetResumeActivityDelegate(bookmark.CallbackMethodName) : WorkflowExecutionContext.Complete;

        // Store the bookmark to resume in the context.
        workflowExecutionContext.ResumedBookmarkContext = new ResumedBookmarkContext(bookmark);

        return workItem;
    }

    /// <summary>
    /// Schedules the specified activity.
    /// </summary>
    public static ActivityWorkItem Schedule(
        this WorkflowExecutionContext workflowExecutionContext,
        ActivityNode activityNode,
        ActivityExecutionContext owner,
        ScheduleWorkOptions? options = default)
    {
        var scheduler = workflowExecutionContext.Scheduler;

        if (options?.PreventDuplicateScheduling == true)
        {
            var existingWorkItem = scheduler.Find(x => x.Activity.Id == activityNode.NodeId);

            if (existingWorkItem != null)
                return existingWorkItem;
        }

        var activity = activityNode.Activity;
        var tag = options?.Tag;
        var workItem = new ActivityWorkItem(activity, owner, tag, options?.Variables, options?.ExistingActivityExecutionContext, options?.Input);
        var completionCallback = options?.CompletionCallback;

        workflowExecutionContext.AddCompletionCallback(owner, activityNode, completionCallback, tag);
        scheduler.Schedule(workItem);

        return workItem;
    }

    /// <summary>
    /// Returns true if all activities have completed or canceled, false otherwise.
    /// </summary>
    public static bool AllActivitiesCompleted(this WorkflowExecutionContext workflowExecutionContext) => workflowExecutionContext.ActivityExecutionContexts.All(x => x.IsCompleted);

    /// <summary>
    /// Adds a new <see cref="WorkflowExecutionLogEntry"/> to the execution log of the current <see cref="WorkflowExecutionContext"/>.
    /// </summary>
    /// <param name="context">The <see cref="WorkflowExecutionContext"/></param> being extended.
    /// <param name="eventName">The name of the event.</param>
    /// <param name="message">The message of the event.</param>
    /// <param name="payload">Any contextual data related to this event.</param>
    /// <returns>Returns the created <see cref="WorkflowExecutionLogEntry"/>.</returns>
    public static WorkflowExecutionLogEntry AddExecutionLogEntry(this WorkflowExecutionContext context, string eventName, string? message = default, object? payload = default)
    {
        var now = context.GetRequiredService<ISystemClock>().UtcNow;

        var logEntry = new WorkflowExecutionLogEntry(
            context.Id,
            default,
            context.Workflow.Id,
            context.Workflow.Type,
            context.Workflow.Identity.Version,
            context.Workflow.Name,
            context.Workflow.Identity.Id,
            default,
            now,
            context.ExecutionLogSequence++,
            eventName,
            message,
            context.Workflow.GetSource(),
            payload);

        context.ExecutionLog.Add(logEntry);
        return logEntry;
    }
}