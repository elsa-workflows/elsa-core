using System.Collections.Generic;
using Elsa.Models;

namespace Elsa.Server.Api.Endpoints.WorkflowDefinitions
{
    public class SaveWorkflowDefinitionRequest
    {
        public string WorkflowDefinitionId { get; set; } = default!;
        public string? Name { get; set; }
        public string? Description { get; set; }
        public Variables? Variables { get; set; }
        public bool IsSingleton { get; set; }
        public WorkflowPersistenceBehavior PersistenceBehavior { get; set; }
        public bool DeleteCompletedInstances { get; set; }
        public bool Enabled { get; set; }
        public bool Publish { get; set; }
        public ICollection<ActivityDefinition> Activities { get; set; } = new List<ActivityDefinition>();
        public ICollection<ConnectionDefinition> Connections { get; set; } = new List<ConnectionDefinition>();
    }
}