using Elsa.Models;
using MediatR;

namespace Elsa.Events
{
    public class WorkflowDefinitionDeleted : INotification
    {
        public WorkflowDefinitionDeleted(WorkflowDefinition workflowDefinition)
        {
            WorkflowDefinition = workflowDefinition;
        }

        public WorkflowDefinition WorkflowDefinition { get; }
    }
}