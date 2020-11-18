using System.Collections.Generic;

namespace Elsa.Models
{
    public interface ICompositeActivityDefinition
    {
        public ICollection<ActivityDefinition> Activities { get; set; }
        public ICollection<ConnectionDefinition> Connections { get; set; }
    }
}