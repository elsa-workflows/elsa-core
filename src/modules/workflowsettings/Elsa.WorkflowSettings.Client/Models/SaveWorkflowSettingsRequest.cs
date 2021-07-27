using System.Runtime.Serialization;

namespace Elsa.WorkflowSettings.Client.Models
{
    [DataContract]
    public sealed class SaveWorkflowSettingsRequest
    {
        [DataMember(Order = 1)] public string? WorkflowSettingsId { get; set; }
        [DataMember(Order = 2)] public string? WorkflowBlueprintId { get; set; }
        [DataMember(Order = 3)] public string? Key { get; set; }
        [DataMember(Order = 4)] public string? Value { get; set; }
    }
}
