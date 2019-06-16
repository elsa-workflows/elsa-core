using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Core.Results;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Results;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Elsa.Core
{
    public class ActivityInvoker : IActivityInvoker
    {
        private readonly IClock clock;
        private readonly ILogger logger;

        public ActivityInvoker(IClock clock, ILogger<ActivityInvoker> logger)
        {
            this.clock = clock;
            this.logger = logger;
        }

        public async Task<ActivityExecutionResult> ExecuteAsync(WorkflowExecutionContext workflowContext, IActivity activity, CancellationToken cancellationToken = default)
        {
            return await InvokeAsync(workflowContext, activity, (a) =>
            {
                workflowContext.Workflow.AddLogEntry(activity.Id, clock.GetCurrentInstant(), "Executing");
                var result = a.ExecuteAsync(workflowContext, cancellationToken);
                workflowContext.Workflow.AddLogEntry(activity.Id, clock.GetCurrentInstant(), "Executed");
                return result;
            });
        }

        public async Task<ActivityExecutionResult> ResumeAsync(WorkflowExecutionContext workflowContext, IActivity activity, CancellationToken cancellationToken = default)
        {
            return await InvokeAsync(workflowContext, activity, (a) =>
            {
                workflowContext.Workflow.AddLogEntry(activity.Id, clock.GetCurrentInstant(), "Resuming");
                var result = a.ResumeAsync(workflowContext, cancellationToken);
                workflowContext.Workflow.AddLogEntry(activity.Id, clock.GetCurrentInstant(), "Resumed");
                return result;
            });
        }

        public async Task<ActivityExecutionResult> HaltedAsync(WorkflowExecutionContext workflowContext, IActivity activity, CancellationToken cancellationToken = default)
        {
            return await InvokeAsync(workflowContext, activity, (a) =>
            {
                workflowContext.Workflow.AddLogEntry(activity.Id, clock.GetCurrentInstant(), "Halting");
                var result = a.HaltedAsync(workflowContext, cancellationToken);
                workflowContext.Workflow.AddLogEntry(activity.Id, clock.GetCurrentInstant(), "Halted");
                return result;
            });
        }

        private async Task<ActivityExecutionResult> InvokeAsync(
            WorkflowExecutionContext workflowContext,
            IActivity activity,
            Func<IActivity, Task<ActivityExecutionResult>> invokeAction)
        {
            try
            {
                return await invokeAction(activity);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while invoking activity {ActivityId} of workflow {WorkflowId}", activity.Id, workflowContext.Workflow.Id);
                workflowContext.Workflow.AddLogEntry(activity.Id, clock.GetCurrentInstant(), e.Message, true);
                return new FaultWorkflowResult(e);
            }
        }
    }
}