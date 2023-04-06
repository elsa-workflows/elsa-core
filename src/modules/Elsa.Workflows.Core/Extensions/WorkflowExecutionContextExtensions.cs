using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Adds extension methods to <see cref="ActivityExecutionContext"/>.
/// </summary>
public static class WorkflowExecutionContextExtensions
{
    /// <summary>
    /// Remove the specified set of <see cref="ActivityExecutionContext"/> from the workflow execution context.
    /// </summary>
    public static async Task RemoveActivityExecutionContextsAsync(this WorkflowExecutionContext workflowExecutionContext, IEnumerable<ActivityExecutionContext> contexts)
    {
        // Copy each item into a new list to avoid changing the source enumerable while removing elements from it.
        var list = contexts.ToList();

        // Remove each context.
        foreach (var context in list) 
            await workflowExecutionContext.RemoveActivityExecutionContextAsync(context);
    }

    /// <summary>
    /// Schedules the workflow for execution.
    /// </summary>
    public static void ScheduleWorkflow(this WorkflowExecutionContext workflowExecutionContext)
    {
        var activityInvoker = workflowExecutionContext.GetRequiredService<IActivityInvoker>();
        var workflow = workflowExecutionContext.Workflow;
        var workItem = new ActivityWorkItem(workflow.Id, async () => await activityInvoker.InvokeAsync(workflowExecutionContext, workflow));
        workflowExecutionContext.Scheduler.Schedule(workItem);
    }
    
    /// <summary>
    /// Schedules the root activity of the workflow.
    /// </summary>
    public static void ScheduleRoot(this WorkflowExecutionContext workflowExecutionContext)
    {
        var activityInvoker = workflowExecutionContext.GetRequiredService<IActivityInvoker>();
        var workflow = workflowExecutionContext.Workflow;
        var workItem = new ActivityWorkItem(workflow.Root.Id, async () => await activityInvoker.InvokeAsync(workflowExecutionContext, workflow.Root));
        workflowExecutionContext.Scheduler.Schedule(workItem);
    }

    /// <summary>
    /// Schedules the specified activity of the workflow.
    /// </summary>
    public static void ScheduleActivity(this WorkflowExecutionContext workflowExecutionContext, IActivity activity)
    {
        var activityInvoker = workflowExecutionContext.GetRequiredService<IActivityInvoker>();
        var workItem = new ActivityWorkItem(activity.Id, async () => await activityInvoker.InvokeAsync(workflowExecutionContext, activity));
        workflowExecutionContext.Scheduler.Schedule(workItem);
    }
    
    /// <summary>
    /// Schedules the specified activity execution context of the workflow.
    /// </summary>
    public static void ScheduleActivityExecutionContext(this WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext)
    {
        var activityInvoker = workflowExecutionContext.GetRequiredService<IActivityInvoker>();
        var workItem = new ActivityWorkItem(activityExecutionContext.Activity.Id, async () => await activityInvoker.InvokeAsync(activityExecutionContext));
        workflowExecutionContext.Scheduler.Schedule(workItem);
    }

    /// <summary>
    /// Schedules the activity of the specified bookmark.
    /// </summary>
    public static void ScheduleBookmark(this WorkflowExecutionContext workflowExecutionContext, Bookmark bookmark)
    {
        // Construct bookmark.
        var bookmarkedActivityContext = workflowExecutionContext.ActivityExecutionContexts.FirstOrDefault(x => x.Id == bookmark.ActivityInstanceId);
        
        if(bookmarkedActivityContext == null)
            return;

        var bookmarkedActivity = bookmarkedActivityContext.Activity;

        // Schedule the activity to resume.
        var activityInvoker = workflowExecutionContext.GetRequiredService<IActivityInvoker>();
        var workItem = new ActivityWorkItem(bookmarkedActivity.Id, async () => await activityInvoker.InvokeAsync(bookmarkedActivityContext));
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
        ActivityCompletionCallback? completionCallback = default,
        object? tag = default)
    {
        var activityInvoker = workflowExecutionContext.GetRequiredService<IActivityInvoker>();
        var workItem = new ActivityWorkItem(activityNode.NodeId, async () => await activityInvoker.InvokeAsync(workflowExecutionContext, activityNode.Activity, owner), tag);
        workflowExecutionContext.Scheduler.Schedule(workItem);
        workflowExecutionContext.AddCompletionCallback(owner, activityNode, completionCallback);
    }

    /// <summary>
    /// Returns true if all activities have completed.
    /// </summary>
    /// <returns>True if all activities have completed, otherwise false.</returns>
    public static bool AllActivitiesCompleted(this WorkflowExecutionContext workflowExecutionContext) => !workflowExecutionContext.ActivityExecutionContexts.Any();
}