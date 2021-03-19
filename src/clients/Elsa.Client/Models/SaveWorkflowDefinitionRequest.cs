﻿using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Elsa.Client.Models
{
    [DataContract]
    public sealed class SaveWorkflowDefinitionRequest
    {
        [DataMember(Order = 1)] public string? WorkflowDefinitionId { get; set; }
        [DataMember(Order = 2)] public string? Name { get; set; }
        [DataMember(Order = 3)] public string? DisplayName { get; set; }
        [DataMember(Order = 4)] public string? Description { get; set; }
        [DataMember(Order = 5)] public Variables? Variables { get; set; }
        [DataMember(Order = 6)] public WorkflowContextOptions? ContextOptions { get; set; }
        [DataMember(Order = 7)] public bool IsSingleton { get; set; }
        [DataMember(Order = 8)] public WorkflowPersistenceBehavior PersistenceBehavior { get; set; }
        [DataMember(Order = 9)] public bool DeleteCompletedInstances { get; set; }
        [DataMember(Order = 10)] public bool Publish { get; set; }
        [DataMember(Order = 11)] public ICollection<ActivityDefinition> Activities { get; set; } = new List<ActivityDefinition>();
        [DataMember(Order = 12)] public ICollection<ConnectionDefinition> Connections { get; set; } = new List<ConnectionDefinition>();
    }
}