using Elsa.Services.Models;

namespace Elsa.Models
{
    public class WorkflowDefinition : CompositeActivityDefinition
    {
        public WorkflowDefinition()
        {
            Variables = new Variables();
        }

        public int Id { get; set; }
        public string WorkflowDefinitionId { get; set; } = default!;
        public string WorkflowDefinitionVersionId { get; set; } = default!;
        public int Version { get; set; }
        public Variables? Variables { get; set; }
        public WorkflowContextOptions? ContextOptions { get; set; }
        public bool IsSingleton { get; set; }
        public WorkflowPersistenceBehavior PersistenceBehavior { get; set; }
        public bool DeleteCompletedInstances { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsPublished { get; set; }
        public bool IsLatest { get; set; }
    }
}