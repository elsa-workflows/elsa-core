using System.Runtime.Serialization;

namespace Elsa.Client.Models
{
    [DataContract]
    public class WorkflowBlueprint : CompositeActivityBlueprint
    {
        [DataMember(Order = 1)] public int Version { get; set; }
        [DataMember(Order = 2)] public string? TenantId { get; set; }
        [DataMember(Order = 3)] public bool IsSingleton { get; set; }
        [DataMember(Order = 4)] public bool IsEnabled { get; set; }
        [DataMember(Order = 5)] public bool IsPublished { get; set; }
        [DataMember(Order = 6)] public bool IsLatest { get; set; }

        /// <summary>
        /// Allows for applications to store an application-specific, queryable value to associate with the workflow.
        /// </summary>
        [DataMember(Order = 7)]
        public string? Tag { get; set; }

        /// <summary>
        /// An initial set of variables available to workflow instances.
        /// </summary>
        [DataMember(Order = 8)]
        public Variables Variables { get; set; } = new();

        /// <summary>
        /// An optional context type around which this workflow revolves. For example, a document, a leave request or a job application.
        /// </summary>
        [DataMember(Order = 9)]
        public WorkflowContextOptions? ContextOptions { get; set; }

        [DataMember(Order = 10)] public WorkflowPersistenceBehavior PersistenceBehavior { get; set; }
        [DataMember(Order = 11)] public bool DeleteCompletedInstances { get; set; }

        /// <summary>
        /// A dictionary to store application-specific properties for a given workflow. 
        /// </summary>
        [DataMember(Order = 12)]
        public Variables CustomAttributes { get; set; } = new();
    }
}