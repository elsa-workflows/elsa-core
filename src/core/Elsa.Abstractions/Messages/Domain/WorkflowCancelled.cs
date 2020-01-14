﻿using Elsa.Services.Models;

namespace Elsa.Messages
{
    /// <summary>
    /// Published when a workflow transitioned into the Cancelled state.
    /// </summary>
    public class WorkflowCancelled : WorkflowNotification
    {
        public WorkflowCancelled(WorkflowExecutionContext workflowExecutionContext) : base(workflowExecutionContext)
        {
        }
    }
}