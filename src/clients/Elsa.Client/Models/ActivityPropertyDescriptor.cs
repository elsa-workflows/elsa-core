using System.Runtime.Serialization;
using Newtonsoft.Json.Linq;

namespace Elsa.Client.Models
{
    [DataContract]
    public class ActivityPropertyDescriptor
    {
        [DataMember(Order = 1)] public string Name { get; set; } = default!;
        [DataMember(Order = 2)] public string UIHint { get; set; } = default!;
        [DataMember(Order = 3)] public string? Label { get; set; }
        [DataMember(Order = 4)] public string? Hint { get; set; }
        [DataMember(Order = 5)] public JToken? Options { get; set; }
        [DataMember(Order = 6)] public string? Category { get; set; }
        [DataMember(Order = 7)] public JToken? DefaultValue { get; set; }
    }
}