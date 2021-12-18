using MediatR;

namespace Elsa.Testing.Events
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
