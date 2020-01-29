using System.Collections.Generic;
using System.Linq;

namespace Elsa.Models
{
    public class WorkflowDefinitionVersion
    {
        public WorkflowDefinitionVersion()
        {
            Variables = new Variables();
            Activities = new List<ActivityDefinition>();
            Connections = new List<ConnectionDefinition>();
        }

        public WorkflowDefinitionVersion(string id,
            string definitionId,
            int version, 
            string name, 
            string description, 
            IEnumerable<ActivityDefinition> activities, 
            IEnumerable<ConnectionDefinition> connections, 
            bool isSingleton, 
            bool isDisabled, 
            Variables variables)
        {
            Id = id;
            DefinitionId = definitionId;
            Version = version;
            Name = name;
            Description = description;
            Activities = activities.ToList();
            Connections = connections.ToList();
            IsSingleton = isSingleton;
            IsDisabled = isDisabled;
            Variables = variables;
        }

        public string? Id { get; set; }
        public string? DefinitionId { get; set; }
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
        public ICollection<ActivityDefinition> Activities { get; set; }
        public ICollection<ConnectionDefinition> Connections { get; set; }
    }
}