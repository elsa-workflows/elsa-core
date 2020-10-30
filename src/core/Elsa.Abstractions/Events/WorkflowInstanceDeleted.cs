using Elsa.Models;
using MediatR;

namespace Elsa.Events
{
    public class WorkflowInstanceDeleted : INotification
    {
        public WorkflowInstanceDeleted(WorkflowInstance workflowInstance)
        {
            WorkflowInstance = workflowInstance;
        }

        public WorkflowInstance WorkflowInstance { get; }
    }
}