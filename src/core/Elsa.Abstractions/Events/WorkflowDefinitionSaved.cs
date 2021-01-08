using Elsa.Models;
using MediatR;

namespace Elsa.Events
{
    public class WorkflowDefinitionSaved : INotification
    {
        public WorkflowDefinitionSaved(WorkflowDefinition workflowDefinition)
        {
            WorkflowDefinition = workflowDefinition;
        }

        public WorkflowDefinition WorkflowDefinition { get; }
    }
}