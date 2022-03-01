using Elsa.Abstractions.Multitenancy;
using Elsa.Services.Models;

namespace Elsa.Events
{
    /// <summary>
    /// Published when a workflow transitioned into the Suspended state.
    /// </summary>
    public class WorkflowSuspended : WorkflowNotification
    {
        public ITenant Tenant { get; }
        public WorkflowSuspended(WorkflowExecutionContext workflowExecutionContext, ITenant tenant) : base(workflowExecutionContext)
        {
            Tenant = tenant;
        }
    }
}