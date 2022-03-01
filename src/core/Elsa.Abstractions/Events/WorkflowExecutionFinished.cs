using Elsa.Abstractions.Multitenancy;
using Elsa.Services.Models;

namespace Elsa.Events
{
    /// <summary>
    /// Published when a burst of execution finished (after status change events have been published as well).
    /// </summary>
    public class WorkflowExecutionFinished : WorkflowNotification
    {

        public WorkflowExecutionFinished(WorkflowExecutionContext workflowExecutionContext, ITenant tenant) : base(workflowExecutionContext)
        {
            Tenant = tenant;
        }

        public ITenant Tenant { get; }
    }
}