using System.Collections.Generic;
using System.Linq;

namespace Elsa.Models
{
    public class WorkflowDefinition
    {
        public WorkflowDefinition()
        {
            Activities = new List<ActivityDefinition>();
            Connections = new List<ConnectionDefinition>();
            Variables = new Variables();
        }

        public WorkflowDefinition(
            string id,
            int version,
            string name,
            string description,
            IEnumerable<ActivityDefinition> activities,
            IEnumerable<ConnectionDefinition> connections,
            bool isSingleton,
            Variables variables) : this(id)
        {
            Version = version;
            Name = name;
            Description = description;
            Activities = activities.ToList();
            Connections = connections.ToList();
            IsSingleton = isSingleton;
            Variables = variables;
        }

        public WorkflowDefinition(string id) : this()
        {
            Id = id;
        }

        public string Id { get; set; }
        public int Version { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ICollection<ActivityDefinition> Activities { get; set; }
        public ICollection<ConnectionDefinition> Connections { get; set; }
        public Variables Variables { get; set; }
        public bool IsSingleton { get; set; }
        public bool IsPublished { get; set; }
    }
}