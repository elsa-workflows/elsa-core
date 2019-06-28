using System.Collections.Generic;
using System.Linq;

namespace Elsa.Models
{
    public class WorkflowDefinition
    {
        public WorkflowDefinition()
        {
        }
        
        public WorkflowDefinition(
            string id,
            IEnumerable<ActivityDefinition> activities,
            IEnumerable<ConnectionDefinition> connections,
            bool isSingleton,
            Variables variables) : this(id)
        {
            Activities = activities.ToList();
            Connections = connections.ToList();
            IsSingleton = isSingleton;
            Variables = variables;
        }

        public WorkflowDefinition(string id)
        {
            Id = id;
            Variables = new Variables();
        }

        public string Id { get; }
        public ICollection<ActivityDefinition> Activities { get; set; }
        public IList<ConnectionDefinition> Connections { get; set; }
        public Variables Variables { get; }
        public bool IsSingleton { get; set; }
    }
}