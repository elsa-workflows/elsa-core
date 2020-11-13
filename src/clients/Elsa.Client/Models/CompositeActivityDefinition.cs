using System.Collections.Generic;

namespace Elsa.Client.Models
{
    public class CompositeActivityDefinition : ActivityDefinition
    {
        public CompositeActivityDefinition()
        {
            Activities = new System.Collections.Generic.List<ActivityDefinition>();
            Connections = new System.Collections.Generic.List<ConnectionDefinition>();
            Type = "CompositeActivity";
        }
        
        public ICollection<ActivityDefinition> Activities { get; set; }
        public ICollection<ConnectionDefinition> Connections { get; set; }
    }
}