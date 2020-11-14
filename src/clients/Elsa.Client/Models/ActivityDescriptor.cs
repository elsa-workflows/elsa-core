using ProtoBuf;

namespace Elsa.Client.Models
{
    [ProtoContract]
    public class ActivityDescriptor
    {
        [ProtoMember(1)] public string Type { get; set; } = default!;
        [ProtoMember(2)] public string DisplayName { get; set; } = default!;
        [ProtoMember(3)] public string? Description { get; set; }
        [ProtoMember(4)] public string? RuntimeDescription { get; set; }
        [ProtoMember(5)] public string Category { get; set; } = default!;
        [ProtoMember(6)] public string? Icon { get; set; }
        [ProtoMember(7)] public string[] Outcomes { get; set; } = new string[0];
        [ProtoMember(8)] public ActivityPropertyDescriptor[] Properties { get; set; } = new ActivityPropertyDescriptor[0];
    }
}