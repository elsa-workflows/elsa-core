using Elsa.Abstractions.Multitenancy;
using MediatR;

namespace Elsa.WorkflowTesting.Events
{
    public class WorkflowTestExecutionStopped : INotification
    {
        public string WorkflowInstanceId { get; }
        public Tenant Tenant { get; }

        public WorkflowTestExecutionStopped(string workflowInstanceId, Tenant tenant)
        {
            WorkflowInstanceId = workflowInstanceId;
            Tenant = tenant;
        }
    }
}
