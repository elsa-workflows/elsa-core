using System.Runtime.Serialization;

namespace Elsa.Client.Models
{
    [DataContract]
    public class WorkflowDispatchResponse
    {
        [DataMember(Order = 1)] public string WorkflowInstanceId { get; set; } = default!;
        [DataMember(Order = 2)] public string? ActivityId { get; set; }
    }
}