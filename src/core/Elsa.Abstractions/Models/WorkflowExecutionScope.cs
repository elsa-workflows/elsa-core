namespace Elsa.Models
{
    public class WorkflowExecutionScope
    {
        public WorkflowExecutionScope()
        {
        }

        public WorkflowExecutionScope(Variables variables)
        {
            Variables = variables;
        }
        
        public Variables Variables { get; }
    }
}