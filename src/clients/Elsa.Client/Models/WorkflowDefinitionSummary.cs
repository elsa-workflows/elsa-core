using System.Runtime.Serialization;

namespace Elsa.Client.Models
{
    public class WorkflowDefinitionSummary
    {
        [DataMember(Order = 1)] public string Id { get; set; } = default!;
        [DataMember(Order = 2)] public string DefinitionId { get; set; } = default!;
        [DataMember(Order = 3)] public string? Name { get; set; }
        [DataMember(Order = 4)] public string? DisplayName { get; set; }
        [DataMember(Order = 5)] public string? Description { get; set; }
        [DataMember(Order = 6)] public int Version { get; set; }
        [DataMember(Order = 7)] public bool IsSingleton { get; set; }
        [DataMember(Order = 8)] public WorkflowPersistenceBehavior PersistenceBehavior { get; set; }
        [DataMember(Order = 9)] public bool IsPublished { get; set; }
        [DataMember(Order = 10)] public bool IsLatest { get; set; }
    }
}