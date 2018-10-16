using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Activities;
using Flowsharp.Models;
using Flowsharp.Results;
using Microsoft.Extensions.Logging;

namespace Flowsharp
{
    public class WorkflowInvoker : IWorkflowInvoker
    {
        public WorkflowInvoker(IActivityInvoker activityInvoker, ILogger<WorkflowInvoker> logger)
        {
            ActivityInvoker = activityInvoker;
            this.logger = logger;
        }

        private readonly ILogger logger;
        public IActivityInvoker ActivityInvoker { get; }

        public async Task<WorkflowExecutionContext> InvokeAsync(Workflow workflow, IActivity startActivity = default, Variables arguments = default, CancellationToken cancellationToken = default)
        {
            workflow.Arguments = arguments ?? new Variables();
            var workflowExecutionContext = new WorkflowExecutionContext(workflow);
            var isResuming = workflowExecutionContext.Workflow.Status == WorkflowStatus.Resuming;

            if (startActivity != null)
                workflow.BlockingActivities.Remove(startActivity);
            else
                startActivity = workflow.Activities.First();
            
            workflowExecutionContext.Workflow.Status = WorkflowStatus.Executing;
            workflowExecutionContext.ScheduleActivity(startActivity);
            
            while (workflowExecutionContext.HasScheduledActivities)
            {
                var currentActivity = workflowExecutionContext.PopScheduledActivity();
                var result = await ExecuteActivityAsync(workflowExecutionContext, currentActivity, isResuming, cancellationToken);
                
                if(result == null)
                    break;
                
                await result.ExecuteAsync(this, workflowExecutionContext, cancellationToken);

                workflowExecutionContext.IsFirstPass = false;
                isResuming = false;
            }

            if(workflowExecutionContext.Workflow.Status != WorkflowStatus.Halted)
                workflowExecutionContext.Finish();
            
            return workflowExecutionContext;
        }

        public Task<WorkflowExecutionContext> ResumeAsync(Workflow workflow, IActivity startActivity = default, Variables arguments = default, CancellationToken cancellationToken = default)
        {
            workflow.Status = WorkflowStatus.Resuming;
            return InvokeAsync(workflow, startActivity, arguments, cancellationToken);
        }

        private async Task<ActivityExecutionResult> ExecuteActivityAsync(WorkflowExecutionContext workflowContext, IActivity activity, bool isResuming, CancellationToken cancellationToken)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    workflowContext.Workflow.Status = WorkflowStatus.Aborted;
                    return null;
                }
                
                return await ExecuteOrResumeActivityAsync(workflowContext, activity, isResuming, cancellationToken);
                
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
            return isResuming
                ? await ActivityInvoker.ResumeAsync(activity, workflowContext, cancellationToken)
                : await ActivityInvoker.ExecuteAsync(activity, workflowContext, cancellationToken);
        }
    }
}
