using System.Runtime.Serialization;

namespace Elsa.Client.Models
{
    [DataContract]
    public class WorkflowDefinition : CompositeActivityDefinition
    {
        public WorkflowDefinition()
        {
            Variables = new Variables();
            Type = "Workflow";
        }

        [DataMember(Order = 1)] public int Id { get; set; }
        [DataMember(Order = 2)] public string WorkflowDefinitionId { get; set; } = default!;
        [DataMember(Order = 3)] public string WorkflowDefinitionVersionId { get; set; } = default!;
        [DataMember(Order = 4)] public int Version { get; set; }
        [DataMember(Order = 5)] public Variables? Variables { get; set; }
        [DataMember(Order = 6)] public WorkflowContextOptions? ContextOptions { get; set; }
        [DataMember(Order = 7)] public bool IsSingleton { get; set; }
        [DataMember(Order = 8)] public WorkflowPersistenceBehavior PersistenceBehavior { get; set; }
        [DataMember(Order = 9)] public bool DeleteCompletedInstances { get; set; }
        [DataMember(Order = 10)] public bool IsEnabled { get; set; }
        [DataMember(Order = 11)] public bool IsPublished { get; set; }
        [DataMember(Order = 12)] public bool IsLatest { get; set; }
    }
}