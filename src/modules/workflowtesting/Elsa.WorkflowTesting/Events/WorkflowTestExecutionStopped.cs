using Elsa.Abstractions.Multitenancy;
using MediatR;

namespace Elsa.WorkflowTesting.Events
{
    public class WorkflowTestExecutionStopped : INotification
    {
        public string WorkflowInstanceId { get; }
        public ITenant Tenant { get; }

        public WorkflowTestExecutionStopped(string workflowInstanceId, ITenant tenant)
        {
            WorkflowInstanceId = workflowInstanceId;
            Tenant = tenant;
        }
    }
}
