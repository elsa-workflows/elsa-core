using Elsa.Models;
using Elsa.Services.Models;
using MediatR;

namespace Elsa.Events
{
    /// <summary>
    /// Published when a workflow instance was cancelled
    /// </summary>
    public class WorkflowInstanceCancelled : INotification
    {
        public WorkflowInstanceCancelled(WorkflowInstance workflowInstance)
        {
            WorkflowInstance = workflowInstance;
        }
        
        public WorkflowInstance WorkflowInstance { get; }
    }
}