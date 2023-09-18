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
    public static void ScheduleWorkflow(this WorkflowExecutionContext workflowExecutionContext)
    {
        var activityInvoker = workflowExecutionContext.GetRequiredService<IActivityInvoker>();
        var workflow = workflowExecutionContext.Workflow;
        var workItem = new ActivityWorkItem(workflow.Id, null, async () => await activityInvoker.InvokeAsync(workflowExecutionContext, workflow));
        workflowExecutionContext.Scheduler.Schedule(workItem);
    }
    
    /// <summary>
    /// Schedules the root activity of the workflow.
    /// </summary>
    public static void ScheduleRoot(this WorkflowExecutionContext workflowExecutionContext)
    {
        var activityInvoker = workflowExecutionContext.GetRequiredService<IActivityInvoker>();
        var workflow = workflowExecutionContext.Workflow;
        var workItem = new ActivityWorkItem(workflow.Root.Id, null, async () => await activityInvoker.InvokeAsync(workflowExecutionContext, workflow.Root));
        workflowExecutionContext.Scheduler.Schedule(workItem);
    }

    /// <summary>
    /// Schedules the specified activity of the workflow.
    /// </summary>
    public static void ScheduleActivity(this WorkflowExecutionContext workflowExecutionContext, IActivity activity)
    {
        var activityInvoker = workflowExecutionContext.GetRequiredService<IActivityInvoker>();
        var workItem = new ActivityWorkItem(activity.Id, null, async () => await activityInvoker.InvokeAsync(workflowExecutionContext, activity));
        workflowExecutionContext.Scheduler.Schedule(workItem);
    }
    
    /// <summary>
    /// Schedules the specified activity execution context of the workflow.
    /// </summary>
    public static void ScheduleActivityExecutionContext(this WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext)
    {
        var activityInvoker = workflowExecutionContext.GetRequiredService<IActivityInvoker>();
        var workItem = new ActivityWorkItem(activityExecutionContext.Activity.Id, null, async () => await activityInvoker.InvokeAsync(activityExecutionContext));
        workflowExecutionContext.Scheduler.Schedule(workItem);
    }

    /// <summary>
    /// Schedules the activity of the specified bookmark.
    /// </summary>
    public static void ScheduleBookmark(this WorkflowExecutionContext workflowExecutionContext, Bookmark bookmark)
    {
        // Get the activity execution context that owns the bookmark.
        var bookmarkedActivityContext = workflowExecutionContext.ActiveActivityExecutionContexts.FirstOrDefault(x => x.Id == bookmark.ActivityInstanceId);
        
        if(bookmarkedActivityContext == null)
            return;

        var bookmarkedActivity = bookmarkedActivityContext.Activity;

        // Schedule the activity to resume.
        var activityInvoker = workflowExecutionContext.GetRequiredService<IActivityInvoker>();
        var workItem = new ActivityWorkItem(bookmarkedActivity.Id, null, async () => await activityInvoker.InvokeAsync(bookmarkedActivityContext));
        workflowExecutionContext.Scheduler.Schedule(workItem);

        // If no resumption point was specified, use "Complete" to prevent the regular "ExecuteAsync" method to be invoked and instead complete the activity.
        workflowExecutionContext.ExecuteDelegate = bookmark.CallbackMethodName != null ? bookmarkedActivity.GetResumeActivityDelegate(bookmark.CallbackMethodName) : WorkflowExecutionContext.Complete;

        // Store the bookmark to resume in the context.
        workflowExecutionContext.ResumedBookmarkContext = new ResumedBookmarkContext(bookmark);
    }

    /// <summary>
    /// Schedules the specified activity.
    /// </summary>
    public static void Schedule(
        this WorkflowExecutionContext workflowExecutionContext,
        ActivityNode activityNode,
        ActivityExecutionContext owner,
        ScheduleWorkOptions? options = default)
    {
        var scheduler = workflowExecutionContext.Scheduler;
        
        if(options?.PreventDuplicateScheduling == true && scheduler.Any(x => x.ActivityId == activityNode.NodeId))
            return;
        
        var activityInvoker = workflowExecutionContext.GetRequiredService<IActivityInvoker>();
        var toolVersion = workflowExecutionContext.Workflow.WorkflowMetadata.ToolVersion;
        var activityId = toolVersion?.Major >= 3 ? activityNode.Activity.Id : activityNode.NodeId;
        var tag = options?.Tag;
        var activityInvocationOptions = new ActivityInvocationOptions(owner, tag, options?.Variables, options?.ReuseActivityExecutionContextId);
        var workItem = new ActivityWorkItem(activityId, owner.Id, async () => await activityInvoker.InvokeAsync(workflowExecutionContext, activityNode.Activity, activityInvocationOptions), tag);
        var completionCallback = options?.CompletionCallback;
        
        workflowExecutionContext.AddCompletionCallback(owner, activityNode, completionCallback, tag);
        scheduler.Schedule(workItem);
        
    }

    /// <summary>
    /// Returns true if all activities have completed or canceled, false otherwise.
    /// </summary>
    public static bool AllActivitiesCompleted(this WorkflowExecutionContext workflowExecutionContext) => workflowExecutionContext.ActiveActivityExecutionContexts.All(x => x.Status != ActivityStatus.Running);
    
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