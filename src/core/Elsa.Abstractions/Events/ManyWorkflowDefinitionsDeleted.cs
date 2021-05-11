using System.Collections.Generic;
using Elsa.Models;
using MediatR;

namespace Elsa.Events
{
    public class ManyWorkflowDefinitionsDeleted : INotification
    {
        public ManyWorkflowDefinitionsDeleted(IEnumerable<WorkflowDefinition> workflowDefinitions) => WorkflowDefinitions = workflowDefinitions;
        public IEnumerable<WorkflowDefinition> WorkflowDefinitions { get; }
    }
}