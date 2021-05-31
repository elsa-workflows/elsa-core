using System.Collections.Generic;
using Elsa.Models;
using MediatR;

namespace Elsa.Events
{
    public class ManyWorkflowDefinitionsDeleting : INotification
    {
        public ManyWorkflowDefinitionsDeleting(IEnumerable<WorkflowDefinition> workflowDefinitions) => WorkflowDefinitions = workflowDefinitions;
        public IEnumerable<WorkflowDefinition> WorkflowDefinitions { get; }
    }
}