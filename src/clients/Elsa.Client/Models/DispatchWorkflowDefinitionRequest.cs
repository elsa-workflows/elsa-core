using System.Runtime.Serialization;

namespace Elsa.Client.Models
{
    [DataContract]
    public sealed class DispatchWorkflowDefinitionRequest
    {
        [DataMember(Order = 1)] public string? ActivityId { get; set; }
        [DataMember(Order = 2)] public string? CorrelationId { get; set; }
        [DataMember(Order = 3)] public string? ContextId { get; set; }
        [DataMember(Order = 4)] public object? Input { get; set; }
    }
}