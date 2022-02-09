using Elsa.Abstractions.MultiTenancy;
using Elsa.Services.Models;

namespace Elsa.Events
{
    /// <summary>
    /// Published when a workflow transitioned into the Suspended state.
    /// </summary>
    public class WorkflowSuspended : WorkflowNotification
    {
        public Tenant Tenant { get; }
        public WorkflowSuspended(WorkflowExecutionContext workflowExecutionContext, Tenant tenant) : base(workflowExecutionContext)
        {
            Tenant = tenant;
        }
    }
}