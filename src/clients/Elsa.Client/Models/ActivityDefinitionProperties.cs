using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Elsa.Client.Models
{
    [DataContract]
    public class ActivityDefinitionProperties : Dictionary<string, ActivityDefinitionPropertyValue>
    {
    }
}