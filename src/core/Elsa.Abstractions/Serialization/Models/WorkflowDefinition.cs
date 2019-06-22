using System.Collections.Generic;
using Elsa.Models;

namespace Elsa.Serialization.Models
{
    public class WorkflowDefinition
    {
        public string Id { get; set; }
        public ICollection<ActivityDefinition> Activities { get; set; }
        public ICollection<Connection> Connections { get; set; }
        public Variables Input { get; set; }
    }
}