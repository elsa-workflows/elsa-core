using System.Runtime.Serialization;
using Newtonsoft.Json.Linq;

namespace Elsa.Client.Models
{
    [DataContract]
    public class ActivityPropertyInfo
    {
        [DataMember(Order = 1)] public string Name { get; set; } = default!;
        [DataMember(Order = 2)] public string Type { get; set; } = default!;
        [DataMember(Order = 3)] public string? Label { get; set; }
        [DataMember(Order = 4)] public string? Hint { get; set; }
        [DataMember(Order = 5)] public JObject? Options { get; set; }
    }
}