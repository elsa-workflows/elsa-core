using System.Runtime.Serialization;

namespace Elsa.Client.Models
{
    [DataContract]
    public class ActivityInfo
    {
        [DataMember(Order = 1)] public string Type { get; set; } = default!;
        [DataMember(Order = 2)] public string DisplayName { get; set; } = default!;
        [DataMember(Order = 3)] public string? Description { get; set; }
        [DataMember(Order = 4)] public string Category { get; set; } = default!;
        [DataMember(Order = 5)] public ActivityTraits Traits { get; set; }
        [DataMember(Order = 6)] public string[] Outcomes { get; set; } = new string[0];
        [DataMember(Order = 7)] public ActivityPropertyInfo[] Properties { get; set; } = new ActivityPropertyInfo[0];
    }
}