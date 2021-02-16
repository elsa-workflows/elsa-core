using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Elsa.Client.Models
{
    [DataContract]
    public class ActivityDefinition
    {
        [DataMember(Order = 1)] public string ActivityId { get; set; } = default!;
        [DataMember(Order = 2)] public string Type { get; set; } = default!;
        [DataMember(Order = 3)] public string? Name { get; set; }
        [DataMember(Order = 4)] public string? DisplayName { get; set; }
        [DataMember(Order = 5)] public string? Description { get; set; }
        [DataMember(Order = 6)] public int? Left { get; set; }
        [DataMember(Order = 7)] public int? Top { get; set; }
        [DataMember(Order = 8)] public bool PersistWorkflow { get; set; }
        [DataMember(Order = 9)] public bool LoadWorkflowContext { get; set; }
        [DataMember(Order = 10)] public bool SaveWorkflowContext { get; set; }
        [DataMember(Order = 11)] public ICollection<ActivityDefinitionProperty> Properties { get; set; } = new List<ActivityDefinitionProperty>();
    }
}