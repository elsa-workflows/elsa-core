using System;
using System.Runtime.Serialization;

namespace Elsa.Client.Models
{
    [DataContract]
    public class ActivityDescriptor
    {
        [DataMember(Order = 1)] public string Type { get; set; } = default!;
        [DataMember(Order = 2)] public string DisplayName { get; set; } = default!;
        [DataMember(Order = 3)] public string? Description { get; set; }
        [DataMember(Order = 4)] public string Category { get; set; } = default!;
        [DataMember(Order = 5)] public ActivityTraits Traits { get; set; }
        [DataMember(Order = 6)] public string[] Outcomes { get; set; } = Array.Empty<string>();
        [DataMember(Order = 7)] public ActivityInputDescriptor[] Properties { get; set; } = Array.Empty<ActivityInputDescriptor>();
        [DataMember(Order = 7)] public ActivityInputDescriptor[] InputProperties { get; set; } = Array.Empty<ActivityInputDescriptor>();
        [DataMember(Order = 7)] public ActivityOutputDescriptor[] OutputProperties { get; set; } = Array.Empty<ActivityOutputDescriptor>();
    }
}