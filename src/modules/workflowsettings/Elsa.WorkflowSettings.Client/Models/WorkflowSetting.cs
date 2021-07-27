using System.Runtime.Serialization;

namespace Elsa.WorkflowSettings.Client.Models
{
    [DataContract]
    public class WorkflowSetting
    {
        [DataMember(Order = 1)] public string Id { get; set; } = default!;

        [DataMember(Order = 3)] public string WorkflowBlueprintId { get; set; } = default!;

        [DataMember(Order = 4)] public string Key { get; set; } = default!;

        [DataMember(Order = 5)] public string Value { get; set; } = default!;
    }
}
