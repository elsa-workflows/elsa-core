using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.ActivityResults;
using Flowsharp.Extensions;
using Flowsharp.Models;
using Microsoft.Extensions.Logging;

namespace Flowsharp.Services
{
    public class WorkflowInvoker : IWorkflowInvoker
    {
        public WorkflowInvoker(ILogger<WorkflowInvoker> logger)
        {
            this.logger = logger;
        }

        private readonly ILogger logger;

        public async Task InvokeAsync(WorkflowExecutionContext workflowContext, string startActivityId, CancellationToken cancellationToken)
        {
            var isResuming = workflowContext.Status == WorkflowStatus.Resuming;
            var startActivity = workflowContext.Activities[startActivityId];

            workflowContext.Status = WorkflowStatus.Executing;
            workflowContext.PushScheduledActivity(startActivity);

            while (workflowContext.HasScheduledActivities)
            {
                var currentActivity = workflowContext.PopScheduledActivity();
                
                if (!await ExecuteActivityAsync(workflowContext, currentActivity, isResuming, cancellationToken))
                    break;

                workflowContext.IsFirstPass = false;
                isResuming = false;
            }

            workflowContext.Status = workflowContext.BlockingActivities.Any() ? WorkflowStatus.Halted : WorkflowStatus.Finished;
        }

        private async Task<bool> ExecuteActivityAsync(WorkflowExecutionContext workflowContext, ActivityExecutionContext activityContext, bool isResuming, CancellationToken cancellationToken)
        {
            try
            {
                await InvokeActivitiesAsync(workflowContext, x => x.ActivityDescriptor.OnActivityExecutingAsync(workflowContext, activityContext, cancellationToken));

                if (cancellationToken.IsCancellationRequested)
                {
                    workflowContext.Status = WorkflowStatus.Aborted;
                    return false;
                }
                
                var result = await ExecuteOrResumeActivityAsync(workflowContext, activityContext, isResuming, cancellationToken);
                await InvokeActivitiesAsync(workflowContext, x => x.ActivityDescriptor.OnActivityExecutedAsync(workflowContext, activityContext, cancellationToken));                
                await result.ExecuteAsync(workflowContext, cancellationToken);
                
            }
            catch (Exception ex)
            {
                FaultWorkflow(workflowContext, activityContext, ex);
            }

            return true;
        }

        private void FaultWorkflow(WorkflowExecutionContext workflowContext, ActivityExecutionContext activityContext, Exception ex)
        {
            logger.LogError(
                ex, 
                "An unhandled error occurred while executing an activity. Workflow ID: '{WorkflowTypeId}'. Activity: '{ActivityId}', '{ActivityName}'. Putting the workflow in the faulted state.", 
                workflowContext.WorkflowType.Name, 
                activityContext.ActivityType.Id, 
                activityContext.ActivityType.Name
            );
            workflowContext.Fault(ex, activityContext);
        }

        private async Task<ActivityExecutionResult> ExecuteOrResumeActivityAsync(WorkflowExecutionContext workflowContext, ActivityExecutionContext activity, bool isResuming, CancellationToken cancellationToken)
        {
            if (!isResuming)
            {
                // Execute the current activity.
                return await activity.ActivityDescriptor.ExecuteActivityAsync(workflowContext, activity, cancellationToken);
            }
            else
            {
                // Resume the current activity.
                return await activity.ActivityDescriptor.ResumeActivityAsync(workflowContext, activity, cancellationToken);
            }
        }

        private async Task InvokeActivitiesAsync(WorkflowExecutionContext workflowContext, Func<ActivityExecutionContext, Task> action)
        {
            await workflowContext.Activities.Values.InvokeAsync(action, logger);
        }
    }
}
