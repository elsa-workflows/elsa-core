using Elsa.Services.Models;
using MediatR;

namespace Elsa.Messages
{
    /// <summary>
    /// Common base for workflow-related events.
    /// </summary>
    public abstract class WorkflowNotification : INotification
    {
        protected WorkflowNotification(Workflow workflow)
        {
            Workflow = workflow;
        }
        
        public Workflow Workflow { get; }
    }
}