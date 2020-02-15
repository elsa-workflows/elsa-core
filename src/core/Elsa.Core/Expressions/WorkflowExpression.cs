namespace Elsa.Expressions
{
    public class WorkflowExpression : IWorkflowExpression
    {
        public WorkflowExpression(string type)
        {
            Type = type;
        }

        public string Type { get; set; }
    }

    public class WorkflowExpression<T> : WorkflowExpression, IWorkflowExpression<T>
    {
        public WorkflowExpression(string type) : base(type)
        {
        }
    }
}