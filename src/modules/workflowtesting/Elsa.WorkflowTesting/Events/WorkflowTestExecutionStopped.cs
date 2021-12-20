using MediatR;

namespace Elsa.WorkflowTesting.Events
{
    public class WorkflowTestExecutionStopped : INotification
    {
        public string WorkflowInstanceId { get; }

        public WorkflowTestExecutionStopped(string workflowInstanceId)
        {
            WorkflowInstanceId = workflowInstanceId;
        }
    }
}
