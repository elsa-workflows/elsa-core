using Elsa.Models;
using MediatR;

namespace Elsa.Events
{
    public class WorkflowDefinitionPublished : INotification
    {
        public WorkflowDefinitionPublished(WorkflowDefinition workflowDefinition)
        {
            WorkflowDefinition = workflowDefinition;
        }

        public WorkflowDefinition WorkflowDefinition { get; }
    }
}