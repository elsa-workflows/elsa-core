namespace Elsa.Models
{
    public class WorkflowExecutionScope
    {
        public WorkflowExecutionScope()
        {
        }

        public WorkflowExecutionScope(Variables variables, string containerActivityId)
        {
            Variables = variables;
            ContainerActivityId = containerActivityId;
        }
        
        public Variables Variables { get; }
        public string ContainerActivityId { get; }
    }
}