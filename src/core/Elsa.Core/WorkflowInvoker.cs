using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Results;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Elsa
{
    public class WorkflowInvoker : IWorkflowInvoker
    {
        private readonly IWorkflowStore workflowStore;
        private readonly IClock clock;
        private readonly ILogger logger;

        public WorkflowInvoker(
            IActivityInvoker activityInvoker,
            IWorkflowStore workflowStore,
            IClock clock,
            ILogger<WorkflowInvoker> logger)
        {
            ActivityInvoker = activityInvoker;
            this.workflowStore = workflowStore;
            this.clock = clock;
            this.logger = logger;
        }

        public IActivityInvoker ActivityInvoker { get; }

        public async Task<WorkflowExecutionContext> InvokeAsync(Workflow workflow, IActivity startActivity = default, Variables arguments = default, CancellationToken cancellationToken = default)
        {
            workflow.Arguments = arguments ?? new Variables();
            var workflowExecutionContext = new WorkflowExecutionContext(workflow);
            var isResuming = workflowExecutionContext.Workflow.Status == WorkflowStatus.Resuming;

            // If a start activity was provided, remove it from the blocking activities list. If not start activity was provided, pick the first one that has no inbound connections.
            if (startActivity != null)
                workflow.BlockingActivities.Remove(startActivity);
            else
                startActivity = workflow.GetStartActivities().FirstOrDefault();

            if (!isResuming)
                workflow.StartedAt = clock.GetCurrentInstant();

            workflowExecutionContext.Workflow.Status = WorkflowStatus.Executing;

            if (startActivity != null)
                workflowExecutionContext.ScheduleActivity(startActivity);

            // Keep executing activities as long as there are any scheduled.
            while (workflowExecutionContext.HasScheduledActivities)
            {
                var currentActivity = workflowExecutionContext.PopScheduledActivity();
                var result = await ExecuteActivityAsync(workflowExecutionContext, currentActivity, isResuming, cancellationToken);

                if (result == null)
                    break;

                await result.ExecuteAsync(this, workflowExecutionContext, cancellationToken);

                workflowExecutionContext.IsFirstPass = false;
                isResuming = false;
            }

            // Any other status than Halted means the workflow has ended (either because it reached the final activity, was aborted or has faulted).
            if (workflowExecutionContext.Workflow.Status != WorkflowStatus.Halted)
            {
                workflowExecutionContext.Finish(clock.GetCurrentInstant());
            }
            else
            {
                // Persist workflow before executing the halted activities.
                await workflowStore.SaveAsync(workflow, cancellationToken);
                
                // Invoke Halted event on activity drivers that halted the workflow.
                while (workflowExecutionContext.HasScheduledHaltingActivities)
                {
                    var currentActivity = workflowExecutionContext.PopScheduledHaltingActivity();
                    var result = await ExecuteActivityHaltedAsync(workflowExecutionContext, currentActivity, cancellationToken);

                    await result.ExecuteAsync(this, workflowExecutionContext, cancellationToken);
                }
            }

            await workflowStore.SaveAsync(workflow, cancellationToken);
            return workflowExecutionContext;
        }

        public Task<WorkflowExecutionContext> ResumeAsync(Workflow workflow, IActivity startActivity = default, Variables arguments = default, CancellationToken cancellationToken = default)
        {
            workflow.Status = WorkflowStatus.Resuming;
            return InvokeAsync(workflow, startActivity, arguments, cancellationToken);
        }

        private async Task<ActivityExecutionResult> ExecuteActivityAsync(WorkflowExecutionContext workflowContext, IActivity activity, bool isResuming, CancellationToken cancellationToken)
        {
            return await ExecuteActivityAsync(workflowContext, activity, () => ExecuteOrResumeActivityAsync(workflowContext, activity, isResuming, cancellationToken), cancellationToken);
        }

        private async Task<ActivityExecutionResult> ExecuteActivityHaltedAsync(WorkflowExecutionContext workflowContext, IActivity activity, CancellationToken cancellationToken)
        {
            return await ExecuteActivityAsync(workflowContext, activity, () => ActivityInvoker.HaltedAsync(workflowContext, activity, cancellationToken), cancellationToken);
        }

        private async Task<ActivityExecutionResult> ExecuteActivityAsync(WorkflowExecutionContext workflowContext, IActivity activity, Func<Task<ActivityExecutionResult>> executeAction, CancellationToken cancellationToken)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    workflowContext.Workflow.Status = WorkflowStatus.Aborted;
                    workflowContext.Workflow.FinishedAt = clock.GetCurrentInstant();
                    return null;
                }

                return await executeAction();
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
            workflowContext.Fault(ex, activity, clock.GetCurrentInstant());
        }

        private async Task<ActivityExecutionResult> ExecuteOrResumeActivityAsync(WorkflowExecutionContext workflowContext, IActivity activity, bool isResuming, CancellationToken cancellationToken)
        {
            return isResuming
                ? await ActivityInvoker.ResumeAsync(workflowContext, activity, cancellationToken)
                : await ActivityInvoker.ExecuteAsync(workflowContext, activity, cancellationToken);
        }
    }
}