using System.Collections.Generic;
using Elsa.Services;

namespace Elsa.Models
{
    public class CompositeActivityDefinition : ActivityDefinition, ICompositeActivityDefinition
    {
        public CompositeActivityDefinition()
        {
            Activities = new List<ActivityDefinition>();
            Connections = new List<ConnectionDefinition>();
            Type = nameof(CompositeActivity);
        }
        
        public ICollection<ActivityDefinition> Activities { get; set; }
        public ICollection<ConnectionDefinition> Connections { get; set; }
    }
}