using System.Collections.Generic;
using System.Runtime.Serialization;
using NodaTime;

namespace Elsa.Client.Models
{
    [DataContract]
    public class WorkflowDefinition
    {
        public WorkflowDefinition()
        {
            Activities = new List<ActivityDefinition>();
            Connections = new List<ConnectionDefinition>();
        }

        [DataMember(Order = 1)] public string Id { get; set; } = default!;
        [DataMember(Order = 2)] public string DefinitionId { get; set; } = default!;
        [DataMember(Order = 3)] public string TenantId { get; set; } = default!;
        [DataMember(Order = 4)] public string? Name { get; set; }
        [DataMember(Order = 5)] public string? DisplayName { get; set; }
        [DataMember(Order = 6)] public string? Description { get; set; }
        [DataMember(Order = 7)] public int Version { get; set; }
        [DataMember(Order = 8)] public string Variables { get; set; } = default!;
        [DataMember(Order = 9)] public string CustomAttributes { get; set; } = default!;
        [DataMember(Order = 10)] public WorkflowContextOptions? ContextOptions { get; set; }
        [DataMember(Order = 11)] public bool IsSingleton { get; set; }
        [DataMember(Order = 12)] public WorkflowPersistenceBehavior PersistenceBehavior { get; set; }
        [DataMember(Order = 13)] public bool DeleteCompletedInstances { get; set; }
        [DataMember(Order = 14)] public bool IsPublished { get; set; }
        [DataMember(Order = 15)] public bool IsLatest { get; set; }

        /// <summary>
        /// Allows for applications to store an application-specific, queryable value to associate with the workflow.
        /// </summary>
        [DataMember(Order = 16)]
        public string? Tag { get; set; }
        
        /// <summary>
        /// The timestamp this workflow definition was created.
        /// </summary>
        [DataMember(Order = 17)]
        public Instant CreatedAt { get; set; }

        [DataMember(Order = 18)] public ICollection<ActivityDefinition> Activities { get; set; }
        [DataMember(Order = 19)] public ICollection<ConnectionDefinition> Connections { get; set; }
    }
}