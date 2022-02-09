using Elsa.Abstractions.MultiTenancy;
using Elsa.Services.Models;

namespace Elsa.Events
{
    /// <summary>
    /// Published when a burst of execution finished (after status change events have been published as well).
    /// </summary>
    public class WorkflowExecutionFinished : WorkflowNotification
    {

        public WorkflowExecutionFinished(WorkflowExecutionContext workflowExecutionContext, Tenant tenant) : base(workflowExecutionContext)
        {
            Tenant = tenant;
        }

        public Tenant Tenant { get; }
    }
}