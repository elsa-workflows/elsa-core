using Elsa.Models;
using MediatR;

namespace Elsa.Events
{
    public class WorkflowInstanceSaved : INotification
    {
        public WorkflowInstanceSaved(WorkflowInstance workflowInstance)
        {
            WorkflowInstance = workflowInstance;
        }

        public WorkflowInstance WorkflowInstance { get; }
    }
}