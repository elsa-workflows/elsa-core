using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Elsa.Client.Models
{
    [DataContract]
    public class CompositeActivityBlueprint : ActivityBlueprint
    {
        [DataMember(Order = 1)] public ICollection<ActivityBlueprint> Activities { get; set; } = new List<ActivityBlueprint>();
        [DataMember(Order = 2)] public ICollection<ConnectionDefinition> Connections { get; set; } = new List<ConnectionDefinition>();
    }
}