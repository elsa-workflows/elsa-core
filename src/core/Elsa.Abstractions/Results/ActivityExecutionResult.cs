﻿using System.Threading;
using System.Threading.Tasks;
 using Elsa.Services.Models;

namespace Elsa.Results
{
    public abstract class ActivityExecutionResult : IActivityExecutionResult
    {
        public virtual Task ExecuteAsync(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken)
        {
            Execute(workflowExecutionContext, activityExecutionContext);
            return Task.CompletedTask;
        }

        protected virtual void Execute(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext)
        {
        }
    }
}
