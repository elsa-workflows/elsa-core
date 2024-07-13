// using Elsa.Extensions;
// using Elsa.Workflows.Contracts;
// using Elsa.Workflows.Management;
// using Elsa.Workflows.Pipelines.WorkflowExecution;
// using Elsa.Workflows.Runtime.Entities;
// using Elsa.Workflows.Runtime.Middleware.Activities;
// using Elsa.Workflows.Runtime.Stimuli;
//
// namespace Elsa.Workflows.Runtime.Middleware.Workflows;
//
// /// <summary>
// /// Schedule background activities.
// /// </summary>
// public class ScheduleBackgroundActivitiesMiddleware(
//     WorkflowMiddlewareDelegate next,
//     IBackgroundActivityScheduler backgroundActivityScheduler,
//     IStimulusHasher stimulusHasher,
//     IBookmarkStore bookmarkStore,
//     IBookmarkManager bookmarkManager,
//     IWorkflowInstanceManager workflowInstanceManager)
//     : WorkflowExecutionMiddleware(next)
// {
//     /// <inheritdoc />
//     public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
//     {
//         await Next(context);
//
//         var cancellationToken = context.CancellationToken;
//         var workflowExecutionContext = context;
//         var tenantId = context.Workflow.Identity.TenantId;
//
//         var scheduledBackgroundActivities = workflowExecutionContext
//             .TransientProperties
//             .GetOrAdd(BackgroundActivityInvokerMiddleware.BackgroundActivitySchedulesKey, () => new List<ScheduledBackgroundActivity>());
//
//         if (scheduledBackgroundActivities.Count == 0)
//             return;
//
//         // Commit state.
//         await workflowInstanceManager.SaveAsync(context, cancellationToken);
//
//         foreach (var scheduledBackgroundActivity in scheduledBackgroundActivities)
//         {
//             // Schedule the background activity.
//             var jobId = await backgroundActivityScheduler.ScheduleAsync(scheduledBackgroundActivity, cancellationToken);
//
//             // Select the bookmark associated with the background activity.
//             var bookmark = workflowExecutionContext.Bookmarks.First(x => x.Id == scheduledBackgroundActivity.BookmarkId);
//             var stimulus = bookmark.GetPayload<BackgroundActivityStimulus>();
//
//             // Store the created job ID.
//             workflowExecutionContext.Bookmarks.Remove(bookmark);
//             //stimulus.JobId = jobId;
//             bookmark = bookmark with
//             {
//                 Payload = bookmark.Payload,
//                 Hash = stimulusHasher.Hash(bookmark.Name, stimulus)
//             };
//             workflowExecutionContext.Bookmarks.Add(bookmark);
//
//             // Update the bookmark.
//             var storedBookmark = new StoredBookmark
//             {
//                 Id = bookmark.Id,
//                 TenantId = tenantId,
//                 ActivityInstanceId = bookmark.ActivityInstanceId,
//                 ActivityTypeName = bookmark.Name,
//                 Hash = bookmark.Hash,
//                 WorkflowInstanceId = workflowExecutionContext.Id,
//                 CreatedAt = bookmark.CreatedAt,
//                 CorrelationId = workflowExecutionContext.CorrelationId,
//                 Payload = bookmark.Payload,
//                 Metadata = bookmark.Metadata,
//             };
//
//             await bookmarkManager.SaveAsync(storedBookmark, cancellationToken);
//         }
//     }
// }