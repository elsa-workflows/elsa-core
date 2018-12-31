using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Results;
using Microsoft.Extensions.Logging;

namespace Elsa
{
    public class WorkflowInvoker : IWorkflowInvoker
    {
        public WorkflowInvoker(IActivityLibrary activityLibrary, ILogger<WorkflowInvoker> logger)
        {
            this.activityLibrary = activityLibrary;
            this.logger = logger;
        }

        private readonly IActivityLibrary activityLibrary;
        private readonly ILogger logger;

        public async Task<WorkflowExecutionContext> InvokeAsync(Workflow workflow, IActivity startActivity = default, Variables arguments = default, CancellationToken cancellationToken = default)
        {
            workflow.Arguments = arguments ?? new Variables();
            var activityDescriptors = await activityLibrary.ListAsync(cancellationToken);
            var activityDescriptorsDictionary = activityDescriptors.ToDictionary(x => x.Name);
            var workflowExecutionContext = new WorkflowExecutionContext(workflow, activityDescriptorsDictionary);
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

        private static async Task<ActivityExecutionResult> ExecuteOrResumeActivityAsync(WorkflowExecutionContext workflowContext, IActivity activity, bool isResuming, CancellationToken cancellationToken)
        {
            var activityContext = workflowContext.CreateActivityExecutionContext(activity);
            return isResuming
                ? await activityContext.Descriptor.ResumeAsync(activityContext, workflowContext, cancellationToken)
                : await activityContext.Descriptor.ExecuteAsync(activityContext, workflowContext, cancellationToken);
        }
    }
}
