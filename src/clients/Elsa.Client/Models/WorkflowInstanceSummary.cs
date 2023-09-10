using System.Runtime.Serialization;
using NodaTime;

namespace Elsa.Client.Models
{
    [DataContract]
    public class WorkflowInstanceSummary
    {
        [DataMember(Order = 0)] public string Id { get; set; } = default!;
        [DataMember(Order = 1)] public string DefinitionId { get; set; } = default!;
        [DataMember(Order = 2)] public string DefinitionVersionId { get; set; } = default!;
        [DataMember(Order = 3)] public string? TenantId { get; set; }
        [DataMember(Order = 4)] public int Version { get; set; }
        [DataMember(Order = 5)] public WorkflowStatus WorkflowStatus { get; set; }
        [DataMember(Order = 6)] public string? CorrelationId { get; set; }
        [DataMember(Order = 7)] public string? ContextType { get; set; }
        [DataMember(Order = 8)] public string? ContextId { get; set; }
        [DataMember(Order = 9)] public string? Name { get; set; }
        [DataMember(Order = 10)] public Instant CreatedAt { get; set; }
        [DataMember(Order = 11)] public Instant? LastExecutedAt { get; set; }
        [DataMember(Order = 12)] public Instant? FinishedAt { get; set; }
        [DataMember(Order = 13)] public Instant? CancelledAt { get; set; }
        [DataMember(Order = 14)] public Instant? FaultedAt { get; set; }
    }
}