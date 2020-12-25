using System.Collections.Generic;

namespace Elsa.Models
{
    public class WorkflowDefinition : Entity, ITenantScope, ICompositeActivityDefinition
    {
        public WorkflowDefinition()
        {
            Variables = new Variables();
            CustomAttributes = new Variables();
            Activities = new List<ActivityDefinition>();
            Connections = new List<ConnectionDefinition>();
        }
        
        public string DefinitionVersionId { get; set; } = default!;
        public string? TenantId { get; set; }
        public string? Name { get; set; }
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        public int Version { get; set; }
        public Variables? Variables { get; set; }
        public Variables? CustomAttributes { get; set; }
        public WorkflowContextOptions? ContextOptions { get; set; }
        public bool IsSingleton { get; set; }
        public WorkflowPersistenceBehavior PersistenceBehavior { get; set; }
        public bool DeleteCompletedInstances { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsPublished { get; set; }
        public bool IsLatest { get; set; }
        public ICollection<ActivityDefinition> Activities { get; set; }
        public ICollection<ConnectionDefinition> Connections { get; set; }
    }
}