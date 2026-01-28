using Elsa.Workflows;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Adds extension methods to <see cref="ActivityExecutionContext"/>.
/// </summary>
public static class WorkflowExecutionContextExtensions
{
    extension(WorkflowExecutionContext workflowExecutionContext)
    {
        /// <summary>
        /// Schedules the workflow for execution.
        /// </summary>
        public ActivityWorkItem ScheduleWorkflow(
            IDictionary<string, object>? input = null,
            IEnumerable<Variable>? variables = null,
            string? schedulingActivityExecutionId = null,
            string? schedulingWorkflowInstanceId = null)
        {
            var workflow = workflowExecutionContext.Workflow;
            var workItem = new ActivityWorkItem(
                workflow,
                input: input,
                variables: variables,
                schedulingActivityExecutionId: schedulingActivityExecutionId,
                schedulingWorkflowInstanceId: schedulingWorkflowInstanceId);
            workflowExecutionContext.Scheduler.Schedule(workItem);
            return workItem;
        }

        /// <summary>
        /// Schedules the root activity of the workflow.
        /// </summary>
        public ActivityWorkItem ScheduleRoot(IDictionary<string, object>? input = null, IEnumerable<Variable>? variables = null)
        {
            var workflow = workflowExecutionContext.Workflow;
            var workItem = new ActivityWorkItem(workflow.Root, input: input, variables: variables);
            workflowExecutionContext.Scheduler.Schedule(workItem);
            return workItem;
        }

        /// <summary>
        /// Schedules the specified activity of the workflow.
        /// </summary>
        public ActivityWorkItem ScheduleActivity(IActivity activity, IDictionary<string, object>? input = null, IEnumerable<Variable>? variables = null)
        {
            var workItem = new ActivityWorkItem(activity, input: input, variables: variables);
            workflowExecutionContext.Scheduler.Schedule(workItem);
            return workItem;
        }

        /// <summary>
        /// Schedules the specified activity execution context of the workflow.
        /// </summary>
        public ActivityWorkItem ScheduleActivityExecutionContext(ActivityExecutionContext activityExecutionContext, IDictionary<string, object>? input = null, IEnumerable<Variable>? variables = null)
        {
            var workItem = new ActivityWorkItem(
                activityExecutionContext.Activity,
                input: input,
                variables: variables,
                existingActivityExecutionContext: activityExecutionContext);
            workflowExecutionContext.Scheduler.Schedule(workItem);
            return workItem;
        }

        /// <summary>
        /// Schedules the activity of the specified bookmark.
        /// </summary>
        /// <returns>The created work item, or <c>null</c> if the specified bookmark doesn't exist in the <see cref="WorkflowExecutionContext"/></returns> 
        public ActivityWorkItem? ScheduleBookmark(Bookmark bookmark, IDictionary<string, object>? input = null, IEnumerable<Variable>? variables = null)
        {
            // Get the activity execution context that owns the bookmark.
            var bookmarkedActivityContext = workflowExecutionContext.ActivityExecutionContexts.FirstOrDefault(x => x.Id == bookmark.ActivityInstanceId);
            var logger = workflowExecutionContext.GetRequiredService<ILogger<WorkflowExecutionContext>>();

            if (bookmarkedActivityContext == null)
            {
                logger.LogWarning("Could not find activity execution context with ID {ActivityInstanceId} for bookmark {BookmarkId}", bookmark.ActivityInstanceId, bookmark.Id);
                return null;
            }

            var bookmarkedActivity = bookmarkedActivityContext.Activity;

            // Schedule the activity to resume.
            var workItem = new ActivityWorkItem(bookmarkedActivity)
            {
                ExistingActivityExecutionContext = bookmarkedActivityContext,
                Input = input ?? new Dictionary<string, object>(),
                Variables = variables
            };
            workflowExecutionContext.Scheduler.Schedule(workItem);

            // If no resumption point was specified, use a "noop" to prevent the regular "ExecuteAsync" method to be invoked and instead complete the activity.
            // Unless the bookmark is configured to auto-complete, in which case we'll just complete the activity.
            workflowExecutionContext.ExecuteDelegate = bookmark.CallbackMethodName != null
                ? bookmarkedActivity.GetResumeActivityDelegate(bookmark.CallbackMethodName)
                : bookmark.AutoComplete
                    ? WorkflowExecutionContext.Complete
                    : WorkflowExecutionContext.Noop;

            // Store the bookmark to resume in the context.
            workflowExecutionContext.ResumedBookmarkContext = new(bookmark);
            logger.LogDebug("Scheduled activity {ActivityId} to resume from bookmark {BookmarkId}", bookmarkedActivity.Id, bookmark.Id);

            return workItem;
        }

        /// <summary>
        /// Schedules the specified activity.
        /// </summary>
        public ActivityWorkItem Schedule(ActivityNode activityNode,
            ActivityExecutionContext owner,
            ScheduleWorkOptions? options = null)
        {
            var schedulerStrategy = workflowExecutionContext.GetRequiredService<IWorkflowExecutionContextSchedulerStrategy>();
            return schedulerStrategy.Schedule(workflowExecutionContext, activityNode, owner, options);
        }

        /// <summary>
        /// Returns true if all activities have completed or canceled, false otherwise.
        /// </summary>
        public bool AllActivitiesCompleted() => workflowExecutionContext.ActivityExecutionContexts.All(x => x.IsCompleted);

        public object? GetOutputByActivityId(string activityId, string? outputName = null)
        {
            var outputRegister = workflowExecutionContext.GetActivityOutputRegister();
            return outputRegister.FindOutputByActivityId(activityId, outputName);
        }

        public IEnumerable<ActivityExecutionContext> FindActivityExecutionContexts(ActivityHandle activityHandle)
        {
            if (activityHandle.ActivityInstanceId != null)
                return workflowExecutionContext.ActivityExecutionContexts.Where(x => x.Id == activityHandle.ActivityInstanceId);
            if (activityHandle.ActivityNodeId != null)
                return workflowExecutionContext.ActivityExecutionContexts.Where(x => x.NodeId == activityHandle.ActivityNodeId);
            if (activityHandle.ActivityId != null)
                return workflowExecutionContext.ActivityExecutionContexts.Where(x => x.Activity.Id == activityHandle.ActivityId);
            if (activityHandle.ActivityHash != null)
            {
                var activity = workflowExecutionContext.FindActivityByHash(activityHandle.ActivityHash);
                return activity != null ? workflowExecutionContext.ActivityExecutionContexts.Where(x => x.Activity.NodeId == activity.NodeId) : [];
            }

            return [];
        }
    }
}