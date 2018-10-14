using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Activities;
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

        public async Task<WorkflowExecutionContext> InvokeAsync(Workflow workflow, IActivity startActivity = default, CancellationToken cancellationToken = default)
        {
            var workflowExecutionContext = new WorkflowExecutionContext(workflow);
            var isResuming = workflowExecutionContext.Status == WorkflowStatus.Resuming;

            if (startActivity == null)
                startActivity = workflow.Activities.First();
            
            workflowExecutionContext.Status = WorkflowStatus.Executing;
            workflowExecutionContext.ScheduleActivity(startActivity);
            await InvokeActivitiesAsync(workflowExecutionContext, x => x.WorkflowStartingAsync(workflowExecutionContext, cancellationToken));
            
            while (workflowExecutionContext.HasScheduledActivities)
            {
                var currentActivity = workflowExecutionContext.PopScheduledActivity();
                var result = await ExecuteActivityAsync(workflowExecutionContext, currentActivity, isResuming, cancellationToken);
                
                if(result == null)
                    break;
                
                await result.ExecuteAsync(workflowExecutionContext, cancellationToken);

                workflowExecutionContext.IsFirstPass = false;
                isResuming = false;
            }

            workflowExecutionContext.Status = WorkflowStatus.Finished;
            
            return workflowExecutionContext;
        }

        private async Task<ActivityExecutionResult> ExecuteActivityAsync(WorkflowExecutionContext workflowContext, IActivity activity, bool isResuming, CancellationToken cancellationToken)
        {
            try
            {
                //await InvokeActivitiesAsync(workflowContext, x => x.ActivityDescriptor.OnActivityExecutingAsync(workflowContext, activity, cancellationToken));

                if (cancellationToken.IsCancellationRequested)
                {
                    workflowContext.Status = WorkflowStatus.Aborted;
                    return null;
                }
                
                return await ExecuteOrResumeActivityAsync(workflowContext, activity, isResuming, cancellationToken);
                //await InvokeActivitiesAsync(workflowContext, x => x.ActivityDescriptor.OnActivityExecutedAsync(workflowContext, activity, cancellationToken));
                
            }
            catch (Exception ex)
            {
                FaultWorkflow(workflowContext, activity, ex);
            }

            return null;
        }

        private void FaultWorkflow(WorkflowExecutionContext workflowContext, IActivity activity, Exception ex)
        {
            logger.LogError(
                ex, 
                "An unhandled error occurred while executing an activity. Putting the workflow in the faulted state."
            );
            workflowContext.Fault(ex, activity);
        }

        private async Task<ActivityExecutionResult> ExecuteOrResumeActivityAsync(WorkflowExecutionContext workflowContext, IActivity activity, bool isResuming, CancellationToken cancellationToken)
        {
            if (!isResuming)
            {
                // Execute the current activity.
                return await activity.ExecuteAsync(workflowContext, new ActivityExecutionContext(activity),  cancellationToken);
            }
            else
            {
                // Resume the current activity.
                return await activity.ResumeAsync(workflowContext, new ActivityExecutionContext(activity), cancellationToken);
            }
        }

        private async Task InvokeActivitiesAsync(WorkflowExecutionContext workflowContext, Func<IActivity, Task> action)
        {
            await workflowContext.Workflow.Activities.InvokeAsync(action, logger);
        }
    }
}
