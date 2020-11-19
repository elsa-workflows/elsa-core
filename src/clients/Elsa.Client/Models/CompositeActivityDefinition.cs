using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Elsa.Client.Models
{
    [DataContract]
    public class CompositeActivityDefinition : ActivityDefinition
    {
        public CompositeActivityDefinition()
        {
            Activities = new List<ActivityDefinition>();
            Connections = new List<ConnectionDefinition>();
            Type = "CompositeActivity";
        }

        [DataMember(Order = 1)] public ICollection<ActivityDefinition> Activities { get; set; }
        [DataMember(Order = 2)] public ICollection<ConnectionDefinition> Connections { get; set; }
    }
}