using Elsa.Extensions;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Pipelines.WorkflowExecution;
using Elsa.Workflows.Runtime.Bookmarks;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Middleware.Activities;
using Elsa.Workflows.Runtime.Models;

namespace Elsa.Workflows.Runtime.Middleware.Workflows;

/// <summary>
/// Schedule background activities.
/// </summary>
public class ScheduleBackgroundActivitiesMiddleware : WorkflowExecutionMiddleware
{
    private readonly IBackgroundActivityScheduler _backgroundActivityScheduler;
    private readonly IStimulusHasher _stimulusHasher;
    private readonly IWorkflowRuntime _workflowRuntime;
    private readonly IWorkflowStateExtractor _workflowStateExtractor;
    
    /// <inheritdoc />
    public ScheduleBackgroundActivitiesMiddleware(
        WorkflowMiddlewareDelegate next,
        IBackgroundActivityScheduler backgroundActivityScheduler,
        IStimulusHasher stimulusHasher,
        IWorkflowRuntime workflowRuntime, 
        IWorkflowStateExtractor workflowStateExtractor) : base(next)
    {
        _backgroundActivityScheduler = backgroundActivityScheduler;
        _stimulusHasher = stimulusHasher;
        _workflowRuntime = workflowRuntime;
        _workflowStateExtractor = workflowStateExtractor;
    }

    /// <inheritdoc />
    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        await Next(context);
        
        var workflowExecutionContext = context;
        var cancellationToken = context.CancellationToken;

        var scheduledBackgroundActivities = workflowExecutionContext
            .TransientProperties
            .GetOrAdd(BackgroundActivityInvokerMiddleware.BackgroundActivitySchedulesKey, () => new List<ScheduledBackgroundActivity>());
  
        if (scheduledBackgroundActivities.Any())
        {
            // Before scheduling background work, ensure the workflow runtime has the current state of the workflow instance.
            await UpdateWorkflowRuntimeStateAsync(workflowExecutionContext, cancellationToken);
        }

        foreach (var scheduledBackgroundActivity in scheduledBackgroundActivities)
        {
            // Schedule the background activity.
            var jobId = await _backgroundActivityScheduler.ScheduleAsync(scheduledBackgroundActivity, cancellationToken);

            // Select the bookmark associated with the background activity.
            var bookmark = workflowExecutionContext.Bookmarks.First(x => x.Id == scheduledBackgroundActivity.BookmarkId);
            var payload = bookmark.GetPayload<BackgroundActivityBookmark>();

            // Store the created job ID.
            workflowExecutionContext.Bookmarks.Remove(bookmark);
            payload.JobId = jobId;
            bookmark = bookmark with
            {
                Payload = bookmark.Payload,
                Hash = _stimulusHasher.Hash(bookmark.Name, payload)
            };
            workflowExecutionContext.Bookmarks.Add(bookmark);
            
            // Update the bookmark.
            var storedBookmark = new StoredBookmark(
                bookmark.Id,
                bookmark.Name,
                bookmark.Hash,
                workflowExecutionContext.Id,
                bookmark.CreatedAt,
                bookmark.ActivityInstanceId,
                workflowExecutionContext.CorrelationId,
                bookmark.Payload
            );
            
            await _workflowRuntime.UpdateBookmarkAsync(storedBookmark, cancellationToken);
        }

        if (scheduledBackgroundActivities.Any())
        {
            // Bookmarks got updated, so we need to update the workflow runtime again with the latest state.
            await UpdateWorkflowRuntimeStateAsync(workflowExecutionContext, cancellationToken);
        }
    }

    private async Task UpdateWorkflowRuntimeStateAsync(WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken)
    {
        var workflowState = _workflowStateExtractor.Extract(workflowExecutionContext);
        await _workflowRuntime.ImportWorkflowStateAsync(workflowState, cancellationToken);
    }
}