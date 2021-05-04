using System.Runtime.Serialization;

namespace Elsa.Client.Models
{
    [DataContract]
    public class ActivityBlueprint
    {
        [DataMember(Order = 1)] public string Id { get; set; } = default!;
        [DataMember(Order = 2)] public string? Name { get; set; }
        [DataMember(Order = 3)] public string? DisplayName { get; set; }
        [DataMember(Order = 4)] public string? Description { get; set;}
        [DataMember(Order = 5)] public string Type { get; set; } = default!;
        [DataMember(Order = 6)] public bool PersistWorkflow { get; set; }
        [DataMember(Order = 7)] public bool LoadWorkflowContext { get; set; }
        [DataMember(Order = 8)] public bool SaveWorkflowContext { get; set; }
        [DataMember(Order = 9)] public Variables Properties { get; set; } = new();
    }
}