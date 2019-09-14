﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Results;
using Elsa.Services.Models;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Elsa.Services
{
    public class ActivityInvoker : IActivityInvoker
    {
        private readonly ILogger logger;

        public ActivityInvoker(ILogger<ActivityInvoker> logger)
        {
            this.logger = logger;
        }

        public async Task<ActivityExecutionResult> ExecuteAsync(WorkflowExecutionContext workflowContext, IActivity activity, CancellationToken cancellationToken = default)
        {
            return await InvokeAsync(workflowContext, activity, (a) =>
            {
                var result = workflowContext.Workflow.Status == WorkflowStatus.Resuming
                    ? a.ResumeAsync(workflowContext, cancellationToken)
                    : a.ExecuteAsync(workflowContext, cancellationToken);
                
                return result;
            });
        }
        public async Task<ActivityExecutionResult> HaltedAsync(WorkflowExecutionContext workflowContext, IActivity activity, CancellationToken cancellationToken = default)
        {
            return await InvokeAsync(workflowContext, activity, (a) =>
            {
                var result = a.HaltedAsync(workflowContext, cancellationToken);
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
                return new FaultWorkflowResult(e);
            }
        }
    }
}