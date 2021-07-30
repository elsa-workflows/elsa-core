using Elsa.Models;
using MediatR;

namespace Elsa.Events
{
    public class WorkflowInputUpdated : INotification
    {
        public WorkflowInputUpdated(WorkflowInstance workflowInstance)
        {
            WorkflowInstance = workflowInstance;
        }

        public WorkflowInstance WorkflowInstance { get; }
    }
}