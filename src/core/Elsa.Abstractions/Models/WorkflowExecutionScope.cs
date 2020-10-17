namespace Elsa.Models
{
    public class WorkflowExecutionScope
    {
        public WorkflowExecutionScope()
        {
            Variables = new Variables();
        }

        public WorkflowExecutionScope(Variables variables)
        {
            Variables = variables;
        }
        
        public Variables Variables { get; }
    }
}