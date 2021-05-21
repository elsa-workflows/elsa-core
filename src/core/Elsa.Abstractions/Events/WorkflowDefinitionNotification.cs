using Elsa.Models;
using MediatR;

namespace Elsa.Events
{
    public abstract class WorkflowDefinitionNotification : INotification
    {
        public WorkflowDefinitionNotification(WorkflowDefinition workflowDefinition) => WorkflowDefinition = workflowDefinition;
        public WorkflowDefinition WorkflowDefinition { get; }
    }
}