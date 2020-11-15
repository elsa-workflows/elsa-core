using System;
using System.Runtime.Serialization;

namespace Elsa.Client.Models
{
    [DataContract]
    public class WorkflowContextOptions
    {
        [DataMember(Order = 1)] public Type ContextType { get; set; } = default!;
        [DataMember(Order = 2)] public WorkflowContextFidelity ContextFidelity { get; set; }
    }
}