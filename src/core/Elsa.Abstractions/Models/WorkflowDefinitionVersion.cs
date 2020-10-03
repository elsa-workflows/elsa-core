using System.Collections.Generic;

namespace Elsa.Models
{
    public class WorkflowDefinitionVersion
    {
        public WorkflowDefinitionVersion()
        {
            Variables = new Variables();
            Activities = new List<ActivityDefinitionRecord>();
            Connections = new List<ConnectionDefinition>();
        }

        public string Id { get; set; } = default!;
        public string DefinitionId { get; set; } = default!;
        public int Version { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public Variables? Variables { get; set; }
        public bool IsSingleton { get; set; }
        public WorkflowPersistenceBehavior PersistenceBehavior { get; set; }
        public bool DeleteCompletedInstances { get; set; }
        public bool IsDisabled { get; set; }
        public bool IsPublished { get; set; }
        public bool IsLatest { get; set; }
        public ICollection<ActivityDefinitionRecord> Activities { get; set; }
        public ICollection<ConnectionDefinition> Connections { get; set; }
    }
}