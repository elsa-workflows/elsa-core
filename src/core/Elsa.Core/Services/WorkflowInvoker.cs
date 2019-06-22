using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Core.Extensions;
using Elsa.Models;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Extensions;
using Elsa.Services.Models;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Elsa.Core.Services
{
    public class WorkflowInvoker : IWorkflowInvoker
    {
        private readonly IActivityInvoker activityInvoker;
        private readonly IEnumerable<IWorkflowEventHandler> workflowEventHandlers;
        private readonly IClock clock;
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger logger;

        public WorkflowInvoker(
            IActivityInvoker activityInvoker,
            IEnumerable<IWorkflowEventHandler> workflowEventHandlers,
            IClock clock,
            IServiceProvider serviceProvider,
            ILogger<WorkflowInvoker> logger)
        {
            this.activityInvoker = activityInvoker;
            this.workflowEventHandlers = workflowEventHandlers;
            this.clock = clock;
            this.serviceProvider = serviceProvider;
            this.logger = logger;
        }

        public async Task<WorkflowExecutionContext> InvokeAsync(
            Workflow workflow,
            IEnumerable<IActivity> startActivities = default,
            CancellationToken cancellationToken = default)
        {
            var workflowExecutionContext = CreateWorkflowExecutionContext(workflow, startActivities);
            await RunWorkflowAsync(workflowExecutionContext, cancellationToken);
            await FinalizeWorkflowExecutionAsync(workflowExecutionContext, cancellationToken);

            return workflowExecutionContext;
        }

        private async Task RunWorkflowAsync(WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken)
        {
            var isResuming = workflowExecutionContext.Workflow.Status == WorkflowStatus.Resuming;

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
        }

        private async Task FinalizeWorkflowExecutionAsync(WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken)
        {
            // Any other status than Halted means the workflow has ended (because it reached the final activity, was aborted or has faulted).
            if (!workflowExecutionContext.Workflow.IsHalted() && !workflowExecutionContext.Workflow.IsFaulted())
            {
                if (workflowExecutionContext.Workflow.BlockingActivities.Any())
                    workflowExecutionContext.Halt(null);
                else
                    workflowExecutionContext.Finish(clock.GetCurrentInstant());
            }
            else
            {
                if (workflowExecutionContext.HasScheduledHaltingActivities)
                {
                    // Notify event handlers that halting activities are about to be executed.
                    await workflowEventHandlers.InvokeAsync(async x => await x.InvokingHaltedActivitiesAsync(workflowExecutionContext, cancellationToken), logger);

                    // Invoke Halted event on activity drivers that halted the workflow.
                    while (workflowExecutionContext.HasScheduledHaltingActivities)
                    {
                        var currentActivity = workflowExecutionContext.PopScheduledHaltingActivity();
                        var result = await ExecuteActivityHaltedAsync(workflowExecutionContext, currentActivity, cancellationToken);

                        await result.ExecuteAsync(this, workflowExecutionContext, cancellationToken);
                    }
                }
            }

            // Notify event handlers that invocation has ended.
            await workflowEventHandlers.InvokeAsync(async x => await x.WorkflowInvokedAsync(workflowExecutionContext, cancellationToken), logger);
        }

        private async Task<ActivityExecutionResult> ExecuteActivityAsync(WorkflowExecutionContext workflowContext, IActivity activity, bool isResuming,
            CancellationToken cancellationToken)
        {
            return await ExecuteActivityAsync(
                workflowContext,
                activity,
                () => ExecuteOrResumeActivityAsync(workflowContext, activity, isResuming, cancellationToken),
                cancellationToken
            );
        }

        private async Task<ActivityExecutionResult> ExecuteActivityAsync(
            WorkflowExecutionContext workflowContext,
            IActivity activity,
            Func<Task<ActivityExecutionResult>> executeAction,
            CancellationToken cancellationToken)
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
        
        private async Task<ActivityExecutionResult> ExecuteActivityHaltedAsync(WorkflowExecutionContext workflowContext, IActivity activity, CancellationToken cancellationToken)
        {
            return await ExecuteActivityAsync(workflowContext, activity, () => activityInvoker.HaltedAsync(workflowContext, activity, cancellationToken), cancellationToken);
        }

        private void FaultWorkflow(WorkflowExecutionContext workflowContext, IActivity activity, Exception ex)
        {
            logger.LogError(ex, "An unhandled error occurred while executing an activity. Putting the workflow in the faulted state.");
            workflowContext.Fault(activity, ex);
        }

        private async Task<ActivityExecutionResult> ExecuteOrResumeActivityAsync(
            WorkflowExecutionContext workflowContext,
            IActivity activity,
            bool isResuming,
            CancellationToken cancellationToken)
        {
            return isResuming
                ? await activityInvoker.ResumeAsync(workflowContext, activity, cancellationToken)
                : await activityInvoker.ExecuteAsync(workflowContext, activity, cancellationToken);
        }

        private WorkflowExecutionContext CreateWorkflowExecutionContext(Workflow workflow, IEnumerable<IActivity> startActivities)
        {
            var workflowExecutionContext = new WorkflowExecutionContext(workflow, clock, serviceProvider);

            // If a start activity was provided, remove it from the blocking activities list. If not start activity was provided, pick the first one that has no inbound connections.
            var startActivityList = startActivities?.ToList();

            if (startActivities != null)
                workflow.BlockingActivities.RemoveWhere(startActivityList.Contains);
            else
                startActivityList = workflow.GetStartActivities().Take(1).ToList();

            if (workflowExecutionContext.Workflow.Status != WorkflowStatus.Resuming)
                workflow.StartedAt = clock.GetCurrentInstant();

            workflowExecutionContext.Workflow.Status = WorkflowStatus.Executing;
            workflowExecutionContext.ScheduleActivities(startActivityList);

            return workflowExecutionContext;
        }
    }
}