using System.Collections.Generic;
using System.Linq;
using Elsa.Models;

namespace Elsa.Services.Models
{
    public class WorkflowBlueprint
    {
        public WorkflowBlueprint(
            IEnumerable<ActivityBlueprint> activities,
            IEnumerable<ConnectionBlueprint> connections,
            Variables variables) : this()
        {
            Activities = activities.ToList();
            Connections = connections.ToList();
            Variables = variables;
        }

        public WorkflowBlueprint()
        {
            Variables = new Variables();
        }

        public string Id { get; set; }
        public ICollection<ActivityBlueprint> Activities { get; }
        public IList<ConnectionBlueprint> Connections { get; }
        public Variables Variables { get; }
    }
}