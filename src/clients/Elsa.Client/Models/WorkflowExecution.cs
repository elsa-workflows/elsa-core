using System.Runtime.Serialization;

namespace Elsa.Client.Models
{
    [DataContract]
    public class WorkflowExecutionResponse
    {
        [DataMember(Order = 1)] public bool Executed { get; set; }
        [DataMember(Order = 2)] public string? ActivityId { get; set; }
        [DataMember(Order = 3)] public WorkflowInstance? WorkflowInstance { get; set; }
    }
}