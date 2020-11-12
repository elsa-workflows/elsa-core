using System.Collections.Generic;

namespace Elsa.Client.Models
{
    public class CompositeActivityDefinition : ActivityDefinition
    {
        public CompositeActivityDefinition()
        {
            Activities = new List<ActivityDefinition>();
            Connections = new List<ConnectionDefinition>();
            Type = "CompositeActivity";
        }
        
        public ICollection<ActivityDefinition> Activities { get; set; }
        public ICollection<ConnectionDefinition> Connections { get; set; }
    }
}