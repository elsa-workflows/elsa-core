using System.Runtime.Serialization;

namespace Elsa.Client.Models
{
    [DataContract]
    public class WorkflowBlueprintSummary
    {
        [DataMember(Order = 0)] public string Id { get; set; } = default!;
        [DataMember(Order = 1)] string VersionId { get; } = default!;
        [DataMember(Order = 2)] public string? Name { get; set; }
        [DataMember(Order = 3)] public string? DisplayName { get; set; }
        [DataMember(Order = 4)] public string? Description { get; set; }
        [DataMember(Order = 5)] public int Version { get; set; }
        [DataMember(Order = 6)] public string? TenantId { get; set; }
        [DataMember(Order = 7)] public bool IsSingleton { get; set; }
        [DataMember(Order = 8)] public bool IsDisabled { get; set; }
        [DataMember(Order = 9)] public bool IsPublished { get; set; }
        [DataMember(Order = 10)] public bool IsLatest { get; set; }
        [DataMember(Order = 11)] string? Tag { get; }
        [DataMember(Order = 12)] string? Channel { get; }
    }
}