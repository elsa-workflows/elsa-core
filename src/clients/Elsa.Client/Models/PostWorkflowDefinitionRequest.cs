using System.Collections.Generic;

namespace Elsa.Client.Models
{
    public class PostWorkflowDefinitionRequest
    {
        public string? Name { get; set; }
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        public Variables? Variables { get; set; }
        public WorkflowContextOptions? ContextOptions { get; set; }
        public bool IsSingleton { get; set; }
        public WorkflowPersistenceBehavior PersistenceBehavior { get; set; }
        public bool DeleteCompletedInstances { get; set; }
        public bool Enabled { get; set; }
        public bool Publish { get; set; }
        public ICollection<ActivityDefinition> Activities { get; set; } = new System.Collections.Generic.List<ActivityDefinition>();
        public ICollection<ConnectionDefinition> Connections { get; set; } = new System.Collections.Generic.List<ConnectionDefinition>();
    }
}