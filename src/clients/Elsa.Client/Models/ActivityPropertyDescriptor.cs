using Newtonsoft.Json.Linq;
using ProtoBuf;

namespace Elsa.Client.Models
{
    [ProtoContract(IgnoreListHandling = true)]
    public class ActivityPropertyDescriptor
    {
        [ProtoMember(1)] public string Name { get; } = default!;
        [ProtoMember(2)] public string Type { get; } = default!;
        [ProtoMember(3)] public string? Label { get; set; }
        [ProtoMember(4)] public string? Hint { get; set; }
        [ProtoMember(5)] public JObject? Options { get; set; }
    }
}