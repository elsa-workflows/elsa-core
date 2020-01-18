﻿using Elsa.Services.Models;
using MediatR;

namespace Elsa.Messaging.Domain
{
    /// <summary>
    /// Common base for workflow-related events.
    /// </summary>
    public abstract class WorkflowNotification : INotification
    {
        protected WorkflowNotification(WorkflowExecutionContext workflowExecutionContext)
        {
            WorkflowExecutionContext = workflowExecutionContext;
        }
        
        public WorkflowExecutionContext WorkflowExecutionContext { get; }
    }
}