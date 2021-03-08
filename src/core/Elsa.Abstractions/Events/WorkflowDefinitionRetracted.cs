using Elsa.Models;
using MediatR;

namespace Elsa.Events
{
    public class WorkflowDefinitionRetracted : INotification
    {
        public WorkflowDefinitionRetracted(WorkflowDefinition workflowDefinition)
        {
            WorkflowDefinition = workflowDefinition;
        }

        public WorkflowDefinition WorkflowDefinition { get; }
    }
}