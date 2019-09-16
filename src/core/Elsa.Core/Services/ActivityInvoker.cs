using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Results;
using Elsa.Services.Models;
using Microsoft.Extensions.Logging;

namespace Elsa.Services
{
    public class ActivityInvoker : IActivityInvoker
    {
        private readonly ILogger logger;

        public ActivityInvoker(ILogger<ActivityInvoker> logger)
        {
            this.logger = logger;
        }

        public async Task<ActivityExecutionResult> ExecuteAsync(
            WorkflowExecutionContext workflowContext,
            IActivity activity,
            CancellationToken cancellationToken = default)
        {
            return await InvokeAsync(
                workflowContext,
                activity,
                (a) => a.ExecuteAsync(workflowContext, cancellationToken)
            );
        }

        public async Task<ActivityExecutionResult> ResumeAsync(
            WorkflowExecutionContext workflowContext,
            IActivity activity,
            CancellationToken cancellationToken = default)
        {
            return await InvokeAsync(
                workflowContext,
                activity,
                (a) => a.ResumeAsync(workflowContext, cancellationToken)
            );
        }

        public async Task<ActivityExecutionResult> HaltedAsync(
            WorkflowExecutionContext workflowContext,
            IActivity activity, CancellationToken cancellationToken = default)
        {
            return await InvokeAsync(
                workflowContext,
                activity,
                (a) => a.HaltedAsync(workflowContext, cancellationToken)
            );
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
                logger.LogError(
                    e,
                    "Error while invoking activity {ActivityId} of workflow {WorkflowId}",
                    activity.Id,
                    workflowContext.Workflow.Id
                );
                return new FaultWorkflowResult(e);
            }
        }
    }
}