using System.Runtime.Serialization;

namespace Elsa.Client.Models
{
    [DataContract]
    public class ActivityBlueprint
    {
        [DataMember(Order = 1)] public string Id { get; set; } = default!;
        [DataMember(Order = 2)] public string? Name { get; set; }
        [DataMember(Order = 3)] public string Type { get; set; } = default!;
        [DataMember(Order = 4)] public bool PersistWorkflow { get; set; }
        [DataMember(Order = 5)] public bool LoadWorkflowContext { get; set; }
        [DataMember(Order = 6)] public bool SaveWorkflowContext { get; set; }
    }
}