namespace Elsa.Models
{
    public class WorkflowDefinitionVersion
    {
        public WorkflowDefinitionVersion()
        {
            Variables = new Variables();
        }

        public string? Id { get; set; }
        public string? DefinitionId { get; set; }
        public int Version { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public ActivityDefinition Start { get; set; }
        public Variables? Variables { get; set; }
        public bool IsSingleton { get; set; }
        public WorkflowPersistenceBehavior PersistenceBehavior { get; set; }
        public bool DeleteCompletedInstances { get; set; }
        
        public bool IsDisabled { get; set; }
        public bool IsPublished { get; set; }
        public bool IsLatest { get; set; }
    }
}